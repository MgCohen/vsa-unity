using System;
using System.IO;
using NUnit.Framework;

namespace VSA.TestSuite.Editor.Tests
{
    public sealed class RecordingSystemTests
    {
        sealed class FakeRecorder : IVideoRecorder
        {
            public int Starts, Stops;
            public bool ThrowOnStop;
            public string Name => "fake";
            public string Unavailable { get; set; }
            public bool IsRecording { get; private set; }
            public event Action<StopReason> Stopped;

            public string Start(string pathNoExtension, RecordingOptions options)
            {
                Starts++;
                IsRecording = true;
                return pathNoExtension + ".mp4";
            }

            public void Stop()
            {
                Stops++;
                IsRecording = false;
                if (ThrowOnStop) throw new InvalidOperationException("encoder failed");
            }

            public void SelfStop(StopReason reason)
            {
                IsRecording = false;
                Stopped?.Invoke(reason);
            }
        }

        FakeRecorder fake;
        RecordingSystem recording;
        string folder;

        [SetUp]
        public void SetUp()
        {
            fake = new FakeRecorder();
            recording = new RecordingSystem(fake);
            folder = Path.Combine(Path.GetTempPath(), "vsa-testsuite-recording-tests");
        }

        [Test]
        public void StopWithNoRecordingDoesNothing()
        {
            Assert.IsNull(recording.Stop());
            Assert.AreEqual(0, fake.Stops);
        }

        [Test]
        public void StopGivesTheReasonAndClearsCurrent()
        {
            recording.Start(folder, "a", "s-1");
            var r = recording.Stop(StopReason.StepFailed);
            Assert.AreEqual(StopReason.StepFailed, r.Reason);
            Assert.IsFalse(r.Active);
            Assert.IsNull(recording.Current);
            Assert.AreSame(r, recording.Last);
        }

        [Test]
        public void NewRecordingReplacesTheRunningOne()
        {
            recording.Start(folder, "first", "s-1");
            recording.Start(folder, "second", "s-2");
            Assert.AreEqual(StopReason.Replaced, recording.Last.Reason);
            Assert.AreEqual("first", recording.Last.Name);
            Assert.AreEqual("second", recording.Current.Name);
        }

        [Test]
        public void SelfStopOfTheAdapterClearsCurrent()
        {
            recording.Start(folder, "a", "s-1");
            fake.SelfStop(StopReason.PlayModeExit);
            Assert.IsNull(recording.Current);
            Assert.AreEqual(StopReason.PlayModeExit, recording.Last.Reason);
        }

        [Test]
        public void FailedStopOfTheAdapterStillClearsCurrent()
        {
            fake.ThrowOnStop = true;
            recording.Start(folder, "a", "s-1");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("encoder failed"));
            recording.Stop();
            Assert.IsNull(recording.Current);
            recording.Start(folder, "b", "s-2");
            Assert.AreEqual("b", recording.Current.Name);
        }

        [Test]
        public void UnavailableRecorderFailsWithNoDevice()
        {
            fake.Unavailable = "no recorder";
            var e = Assert.Throws<ActionFailed>(() => recording.Start(folder, "a", "s-1"));
            Assert.AreEqual(FailCode.NoDevice, e.Code);
        }

        [Test]
        public void TimeLimitOverrideDoesNotChangeTheDefault()
        {
            recording.Start(folder, "a", "s-1", maxSeconds: 2);
            Assert.AreEqual(600, recording.Options.MaxSeconds);
        }
    }
}
