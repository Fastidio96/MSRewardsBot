using System;

namespace MSRewardsBot.Server.DataEntities.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequiredPrivilegeAttribute : Attribute
    {
    }
}
