using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKS
{
    internal class Item
    {
        public string Name { get; set; }

        public Item(string name)
        {
            Name = name;
        }

        public bool equals(Object obj)
        {
            if (obj is Item)
            {
                Item item = (Item)obj;
                return (item.Name == this.Name);
            }
            return false;
        }

    }
}
