using System.Collections.Generic;

namespace Citizens
{
    public class Family
    {
        public List<FamilyMember> Members { get; } = new List<FamilyMember>();

        public void AddMember(FamilyMember member)
        {
            Members.Add(member);
        }

        public void RemoveMember(FamilyMember member)
        {
            Members.Remove(member);
        }
    }

    public class FamilyMember
    {
        public Agent Agent { get; }
    }
}