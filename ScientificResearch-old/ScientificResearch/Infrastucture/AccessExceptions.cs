using System;

namespace ScientificResearch.Infrastucture;

/// <summary>已登录人员缺少目标资源或流程操作权限。</summary>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message) { }
}
