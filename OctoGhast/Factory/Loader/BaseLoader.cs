using System;
using Newtonsoft.Json.Linq;

namespace OctoGhast {
    public class BaseLoader {
        /*
         * Given Object A:
         * {
           "abstract": "flesh",
           "type": "COMESTIBLE",
           "comestible_type": "FOOD",
           "name": "meat/fish",
           "material": "flesh",
           "symbol": "%",
           "price": 500,
           "color": "red",
           "parasites": 32,
           "vitamins": [ [ "calcium", 1 ], [ "iron", 20 ], [ "vitB", 10 ] ],
           "rot_spawn": "GROUP_CARRION"
           }
         * and Object B:
         * {
           "id": "meat",
           "copy-from": "flesh",
           "type": "COMESTIBLE",
           "name": "chunk of meat",
           "name_plural": "chunks of meat",
           "description": "Freshly butchered meat.  You could eat it raw, but cooking it is better.",
           "weight": 200,
           "volume": 1,
           "proportional": { "price": 1.5 },
           "spoils_in": 24,
           "calories": 250,
           "healthy": -1,
           "fun": -10,
           "extend": { "flags" : [ "SMOKABLE" ] }
           }
         * Because of 'copy-from', we treat A & B specially, A is an 'abstract' item so it doesn't get
         * to exist in game as a unique item of it's own, it's a template for templates.
         *
         * ObjectB should be created as a copy of A. With any properties from B overriding those in A
         * and any properties appearing in a 'Proportional', 'Relative', 'Extend' or 'Delete' should be
         * handled transparently.
         *
         * We should be able to handle this neatly via the following:
         *
         * - Core::ItemFactory handles all ITEM + [legacy_types] loads
         * - Core::DataLoader provides a number of Assign<T>(JObject data, string name, Func<T> property)
         * - Using LegacyLoaders::ComestibleLoader as an example:
         *      - Core::ItemFactory receives a template with the type of 'COMESTIBLE'
         *      - Core::ItemFactory inspects the object for 'copy-from'
         *      - if 'copy-from' is found and has a value:
         *          - Core::ItemFactory.Items, containing already instantiated items, is queried
         *          - Core::ItemFactory.Abstracts, containing known abstract items, is queried
         *      - If either of those lists contains the Item of interest:
         *          - Core::ItemFactory copies the itype (Json serialize/deserialize)
         *          - Core::ItemFactory then passes the copy onto the individual Loader
         *      - If they don't, a new instance of itype is given to the individual loader
         *      - The loader uses CoreBaseLoader::Assign<T>()
         *          - Core::BaseLoader::Assign<T>() handles Proportional/Relative/Extend/Delete as appropriate
         *              for the type of T.
         *          - If the requested property doesn't exist on B, return false to indicate no property was set.
         *          - The idea here is that when dealing with a new itype, the value will remain as default.
         *               and when dealing with a rehydrated copy itype, the value is left as previously defined
         *          - All loaders must implement Check() to confirm 
         */
    }
}