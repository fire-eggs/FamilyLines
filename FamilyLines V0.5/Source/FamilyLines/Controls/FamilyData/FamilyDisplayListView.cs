/*
 * Family.Show derived code provided under MS-PL license.
 */
/*
 * Derived class that filters data in the diagram view.
*/

using KBS.FamilyLinesLib;

namespace KBS.FamilyLines
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    class FamilyDisplayListView : FilterSortListView
    {
        /// <summary>
        /// Called for each item in the list. Return true if the item should be in
        /// the current result set, otherwise return false to exclude the item.
        /// </summary>
        protected override bool FilterCallback(object item)
        {
            Person person = item as Person;
            if (person == null)
                return false;

            if (this.Filter.Matches(person.Name) ||
                this.Filter.MatchesYear(person.BirthDate) ||
                this.Filter.MatchesYear(person.DeathDate) ||
                this.Filter.Matches(person.Age))
                return true;

            return false;
        }
    }
}
