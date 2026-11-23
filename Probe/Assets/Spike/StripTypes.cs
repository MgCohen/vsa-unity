using System;

// S1 to S6: what UnityLinker keeps. No code refers to these types. The check finds them with GetTypes().
// Each tree has a base with a marker and a child with no marker.
namespace Spike.Strip {
    // S1: no marker (control).
    public class NoneBase {
    }

    public class NoneChild : NoneBase {
    }

    public sealed class NoneGrandchild : NoneChild {
    }

    // S2: the real Unity [Preserve].
    [UnityEngine.Scripting.Preserve]
    public class RealBase {
    }

    public class RealChild : RealBase {
    }

    public sealed class RealGrandchild : RealChild {
    }

    // S3: an attribute with a different name and no [Preserve] base.
    public sealed class KeepAttribute : Attribute {
    }

    [Keep]
    public class KeepBase {
    }

    public class KeepChild : KeepBase {
    }

    public sealed class KeepGrandchild : KeepChild {
    }

    // S6: the real Unity [RequireDerived], for comparison.
    [UnityEngine.Scripting.RequireDerived]
    public class DerivedBase {
    }

    public class DerivedChild : DerivedBase {
    }

    public sealed class DerivedGrandchild : DerivedChild {
    }
}

// S4: an attribute with the same name that inherits the Unity [Preserve].
namespace Spike.Strip.Same {
    public sealed class PreserveAttribute : UnityEngine.Scripting.PreserveAttribute {
    }

    [Preserve]
    public class SameBase {
    }

    public class SameChild : SameBase {
    }

    public sealed class SameGrandchild : SameChild {
    }
}

// S5: an own copy with the same name and no Unity base, as Umpire.Markers does it.
namespace Spike.Strip.Copy {
    public sealed class PreserveAttribute : Attribute {
    }

    [Preserve]
    public class CopyBase {
    }

    public class CopyChild : CopyBase {
    }

    public sealed class CopyGrandchild : CopyChild {
    }
}

// S7: the real Unity [RequireDerived] on a base that code uses (SpikeChecks.Kept refers to UsedBase).
namespace Spike.Strip {
    [UnityEngine.Scripting.RequireDerived]
    public class UsedBase {
    }

    public class UsedChild : UsedBase {
    }

    public sealed class UsedGrandchild : UsedChild {
    }
}

// S8: an attribute with a different name and no [Preserve] base.
namespace Spike.Strip.Own {
    public class PreserveAttribute : System.Attribute {
    }

    public sealed class AutoKeepAttribute : System.Attribute {
    }

    [AutoKeep]
    public class OwnBase {
    }

    public class OwnChild : OwnBase {
    }

    public sealed class OwnGrandchild : OwnChild {
    }
}

// S9: the Unity [Preserve] on the base, and code uses the child (SpikeChecks.Kept refers to UsedPreserveChild).
namespace Spike.Strip {
    [UnityEngine.Scripting.Preserve]
    public class UsedPreserveBase {
    }

    public class UsedPreserveChild : UsedPreserveBase {
    }

    public sealed class UsedPreserveGrandchild : UsedPreserveChild {
    }
}
