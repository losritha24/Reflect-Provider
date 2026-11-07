using Reflect.ProviderDashboard.Domain.Enums;

namespace Reflect.ProviderDashboard.Domain.Catalog;

public enum MobilePlan
{
    Free = 1,
    Basic = 2,
    Premium = 3
}

public sealed record ReflectModule(
    string Name,
    string Group,
    TherapyPlatform Platform,
    bool UsesAi = false,
    MobilePlan? MinimumPlan = null);

/// <summary>
/// Official consumer module list (Product Catalog, effective 2026-09-09).
/// Sessions must name a module from this list for the platform they ran on.
/// </summary>
public static class ReflectModuleCatalog
{
    public static IReadOnlyList<ReflectModule> All { get; } = Build();

    public static IReadOnlyList<ReflectModule> For(TherapyPlatform platform) =>
        All.Where(module => module.Platform == platform).ToList();

    public static ReflectModule Find(TherapyPlatform platform, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var wanted = name.Trim();
        return All.FirstOrDefault(module =>
            module.Platform == platform && module.Name.Equals(wanted, StringComparison.OrdinalIgnoreCase));
    }

    private static List<ReflectModule> Build()
    {
        const TherapyPlatform m = TherapyPlatform.Mobile;
        const TherapyPlatform x = TherapyPlatform.Vr;
        return
        [
            new("Floating Bubbles", "Creative Calm", m, MinimumPlan: MobilePlan.Free),
            new("Finger Painting", "Creative Calm", m, MinimumPlan: MobilePlan.Free),
            new("Pour Paint", "Creative Calm", m, MinimumPlan: MobilePlan.Free),
            new("Breathing Mandala", "Creative Calm", m, MinimumPlan: MobilePlan.Free),
            new("Photo Coloring", "Creative Calm", m, true, MobilePlan.Basic),
            new("AI Created Mandala", "Creative Calm", m, true, MobilePlan.Premium),
            new("Create Your Own Prompt", "Create", m, true, MobilePlan.Basic),
            new("Options Prompt Pathway", "Create", m, true, MobilePlan.Basic),
            new("Select from Images Pathway", "Create", m, true, MobilePlan.Basic),
            new("Creativity", "Learn", m, true, MobilePlan.Premium),
            new("Emotions", "Learn", m, true, MobilePlan.Premium),
            new("Self", "Learn", m, true, MobilePlan.Premium),
            new("Journey", "Learn", m, true, MobilePlan.Premium),
            new("Reflection Journals", "Reflect", m, true, MobilePlan.Basic),

            new("Containment", "Creative Calm", x),
            new("Rhythmic Relaxation", "Creative Calm", x),
            new("Color Psychology", "Creative Calm", x),
            new("Flow", "Creative Calm", x),
            new("Emotional Expression", "Explore Emotions", x),
            new("Self Compassion", "Explore Emotions", x),
            new("Catharsis", "Explore Emotions", x),
            new("Portal to the Subconscious", "Explore Emotions", x),
            new("Present Self Outside", "Explore Self", x),
            new("Present Self Inside", "Explore Self", x),
            new("Personal Strengths", "Explore Self", x),
            new("Future Self", "Explore Self", x),
            new("Appreciation of Life", "Explore Journey", x),
            new("Hope Tree", "Explore Journey", x),
            new("New Pathways", "Explore Journey", x),
            new("Finding Balance", "Explore Journey", x),
            new("Training Tutorial", "Getting Started", x)
        ];
    }
}
