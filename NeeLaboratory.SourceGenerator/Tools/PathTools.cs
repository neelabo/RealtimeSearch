using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace NeeLaboratory.SourceGenerator.Tools;

public static class PathTools
{
    public static string ReplaceInvalidFileNameChars(string s)
    {
        if (s is null) return "";

        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return string.Concat(s.Select(c => invalidChars.Contains(c) ? '_' : c));
    }
}