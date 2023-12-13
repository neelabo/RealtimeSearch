#pragma warning disable RS2008 // アナライザー リリース追跡を有効にする
using Microsoft.CodeAnalysis;

namespace NeeLaboratory.SourceGenerator;

public static class DiagnosticDescriptors
{
    const string Category = "NeeLaboratory.SourceGenerator";

    // INotifyPropertyChanged が必要
    public static readonly DiagnosticDescriptor INotifyPropertyChangedRequired = new(
        id: "NSG1001",
        title: "INotifyPropertyChanged required",
        messageFormat: "class '{0}' requires INotifyPropertyChanged",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
