using System.Collections.Generic;
using Capsicum.Interfaces;

namespace OctoGhast.Activities {
    public class ReadingLedgerComponent : IComponent {
        /// <summary>
        /// Things that have been read completely
        /// </summary>
        public List<string> FullyRead { get; set; }

        /// <summary>
        /// Things that have been partially read
        /// </summary>
        public List<string> PartiallyRead { get; set; }

        /// <summary>
        /// Things that have been partially read, and how far into them (in pages) they have been read.
        /// </summary>
        public Dictionary<string,int> PartiallyReadData { get; set; }

        /// <summary>
        /// Things that have been skimmed for the summary, recipes contained, metainfo, etc.
        /// The exact result of skimming depends on the type of book.
        /// </summary>
        public List<string> Skimmed { get; set; }
    }
}