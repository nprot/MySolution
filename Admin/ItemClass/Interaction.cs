using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ES
{
    public class Item
    {
        public string id { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public int price { get; set; }
        public int amount { get; set; }
        public Item(string _Id, string _Name, int _Price, int _Amount, string _Desc)
        {
            name = _Name;
            id = _Id;
            price = _Price;
            amount = _Amount;
            desc = _Desc;
        }
        public override string ToString()
        {
            return "Id:  " + id + " | Name: " + name + " | Price: " + price + " | Amount: " + amount + " | Desc : " + desc;
        }
        public void print()
        {
            Console.WriteLine(ToString());
        }
        public static void deleteItems(ElasticClient client)
        {
            var res = client.Search<Item>(s => s.From(0).Size(10000));
            foreach (var hit in res.Hits)
            {
                deleteItem(client, hit.Source.id, hit.Source.amount);
            }
            client.Refresh();
        }
        public static void addNewItem(ElasticClient client, Item item)
        {
            client.Index(item, p => p
            .Id(item.id));
            client.Refresh();
        }
        public static bool deleteItem(ElasticClient client, string id, int amount)
        {
            bool exists = isExists(client, id, amount);
            if (!exists) return false;

            Item oldItem = client.Get<Item>(g => g
            .Id(id)).Source;
            Item item = new Item(id, oldItem.name, oldItem.price, oldItem.amount - amount, oldItem.desc);

            if (item.amount > 0)
            {
                addNewItem(client, item);
            }
            else
            {
                client.Delete<Item>(g => g
                .Id(item.id));
            }
            client.Refresh();
            return true;
        }

        public static void showItems(ElasticClient client)
        {
            var res = client.Search<Item>(s => s.From(1).Size(10000).Query(q => q.MatchAll()).SortAscending(o => Convert.ToInt32(o.id)));
            foreach (var hit in res.Hits)
            {
                hit.Source.print();
            }
            client.Refresh();
        }
        public static void showProfit(ElasticClient client)
        {
            Console.WriteLine("Profit: "+ client.Get<Item>(g => g.Id(0)).Source.price);
        }
        public static bool isExists(ElasticClient client, string id, int amount)
        {
            var resGet = client.Get<Item>(g => g
            .Id(id));

            if (!resGet.Found) return false;
            if (resGet.Source.amount < amount) return false;
            client.Refresh();
            return true;
        }
        public static bool addExistItem(ElasticClient client, string id, int amount)
        {
            if (!isExists(client, id, 0)) { return false; }
            Item oldItem = client.Get<Item>(g => g
            .Id(id)).Source;
            addNewItem(client, new Item(oldItem.id, oldItem.name, oldItem.price, oldItem.amount + amount, oldItem.desc));
            client.Refresh();
            return true;
        }
        public static bool buyItem(ElasticClient client, string id, int amount)
        {
            if (!isExists(client, id, amount)) return false;
            Item zeroIt = client.Get<Item>(g => g.Id("0")).Source;
            int profit = zeroIt.price + amount * client.Get<Item>(g => g
                .Id(id)).Source.price;
            //Console.WriteLine("My Profit: " + profit);
            addNewItem(client, new Item("0",zeroIt.name,profit,zeroIt.amount,zeroIt.desc));
            deleteItem(client, id, amount);
            return true;
        }
    }
}
