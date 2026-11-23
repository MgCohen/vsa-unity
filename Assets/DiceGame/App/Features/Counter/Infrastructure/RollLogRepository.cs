using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiceGame.Features.Counter.Domain;
using Umpire.Vsa.Infrastructure;

namespace DiceGame.Features.Counter.Infrastructure {
    sealed class RollLogRepository : IRepository<RollLog, int> {
        readonly RollLogStore store;
        readonly List<(RollLog Log, bool IsAdd)> pending = new List<(RollLog Log, bool IsAdd)>();

        public RollLogRepository(RollLogStore store) => this.store = store;

        public Task<RollLog> GetAsync(int id, CancellationToken ct) =>
            Task.FromResult(store.Logs.TryGetValue(id, out var log) ? log : null);

        public Task<List<RollLog>> ListAsync(Func<RollLog, bool> where, CancellationToken ct) =>
            Task.FromResult(store.Logs.Values.Where(where).ToList());

        public void Add(RollLog entity) => pending.Add((entity, true));

        public void Remove(RollLog entity) => pending.Add((entity, false));

        public Task SaveChangesAsync(CancellationToken ct) {
            foreach (var (log, isAdd) in pending) {
                if (isAdd) {
                    store.Logs[log.Id] = log;
                } else {
                    store.Logs.TryRemove(log.Id, out _);
                }
            }
            pending.Clear();
            return Task.CompletedTask;
        }
    }
}
