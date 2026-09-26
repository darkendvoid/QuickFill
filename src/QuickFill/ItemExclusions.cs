using System.Collections.Generic;
using System.Linq;

namespace QuickFill
{
    /// <summary>
    /// The "Excluded items" setting. Entries match an item's prefab name ("RoundLog") or its localized
    /// in-game name ("Core wood"), ignoring case, spaces and punctuation.
    /// </summary>
    internal sealed class ItemExclusions
    {
        private readonly HashSet<string> _names;

        public ItemExclusions(string setting)
        {
            _names = new HashSet<string>(setting.Split(',').Select(Normalize).Where(n => n.Length > 0));
        }

        public bool Contains(ItemDrop item)
        {
            if (_names.Count == 0)
                return false;
            return _names.Contains(Normalize(item.name))
                || _names.Contains(Normalize(Localization.instance.Localize(item.m_itemData.m_shared.m_name)));
        }

        private static string Normalize(string name)
        {
            return new string(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }
    }
}
