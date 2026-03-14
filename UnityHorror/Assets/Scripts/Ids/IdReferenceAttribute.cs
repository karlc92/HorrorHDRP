using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public sealed class IdReferenceAttribute : PropertyAttribute
{
    public Type SourceType { get; }
    public string IdMemberName { get; }
    public IdReferenceScope Scope { get; }

    public IdReferenceAttribute(Type sourceType, string idMemberName, IdReferenceScope scope = IdReferenceScope.SceneObjects)
    {
        SourceType = sourceType;
        IdMemberName = idMemberName;
        Scope = scope;
    }
}
