#if UNITY_EDITOR
namespace Mindforge.Editor
{
    /// <summary>
    /// Stable editor-namespace facade for the presentation budget audit.
    /// Historical qualification tooling owns the implementation in Mindforge.EditorTools.
    /// </summary>
    public static class PresentationBudgetAudit
    {
        public static void Run()
        {
            Mindforge.EditorTools.PresentationBudgetAudit.Run();
        }
    }
}
#endif
