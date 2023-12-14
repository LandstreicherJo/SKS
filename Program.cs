using SKS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Transactions;
using System.Xml;
using System.Xml.Linq;

namespace SKS // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Random rnd = new Random();
            List<List<Item>> transactions = new List<List<Item>>();

            List<Item> items = new List<Item>();
            items.Add(new Item("Milch"));
            items.Add(new Item("Schokolade"));
            items.Add(new Item("Nudeln"));
            items.Add(new Item("Reis"));
            items.Add(new Item("Brot"));

            //transactions
            for (int i = 0; i < 100; i++)
            {
                List<Item> transaction = new List<Item>();
                if (rnd.Next() % 8 > 2) transaction.Add(items[0]);
                if (rnd.Next() % 2 == 0) transaction.Add(items[1]);
                if (rnd.Next() % 8 > 2) transaction.Add(items[2]);
                if (rnd.Next() % 2 == 0) transaction.Add(items[3]);
                if (rnd.Next() % 2 == 0) transaction.Add(items[4]);
                if (transaction.Count == 0)
                {
                    i--;
                }
                else
                {
                    transactions.Add(transaction);
                }
            }
            Console.WriteLine(TransactionsToString(transactions) + "\n\n\n");
            Console.WriteLine(CreateAprioriAssociations(transactions).Name);

        }
        //----------------------To String Methods------------------------------------------------------------------------
        private static string TransactionsToString(List<List<Item>> transactions)
        {
            string output = string.Empty;
            int index = 1;
            foreach (List<Item> transaction in transactions)
            {
                output += index + ". " + TransactionToString(transaction) + "\n";
                index++;
            }
            return output;
        }

        private static string TransactionToString(List<Item> transaction)
        {
            string output = string.Empty;
            foreach (Item item in transaction)
            {
                output += item.Name + ", ";
            }
            return output;
        }

        private static string DictionaryToString(Dictionary<Item, int> keyValuePairs)
        {
            string output = string.Empty;
            foreach (Item item in keyValuePairs.Keys)
            {
                output += item.Name + " | " + keyValuePairs[item] + "\n";
            }
            return output;
        }

        private static string CombinationsToString(List<List<Item>> combinations)
        {
            string output = string.Empty;
            foreach (var combination in combinations)
            {
                output += "Key: ";
                foreach (Item item in combination)
                {
                    output += item.Name + "  ";
                }
                output += "\n";
            }
            return output;
        }

        //----------------------------------------------------------------------------------------------
        private static Item CreateAprioriAssociations(List<List<Item>> transactions)
        {
            float minimalSupport = transactions.Count * 0.15f;
            float confidenceLevel = 0.7f;
            Console.WriteLine("minimalSupport = " + minimalSupport + "\nconfidenceLevel = " + confidenceLevel + "\n\n\n");

            //Step 1
            //Join - K=1
            Dictionary<Item, int> frequentItemsets = new Dictionary<Item, int>();
            foreach (List<Item> transaction in transactions)
            {
                foreach (Item item in transaction)
                {
                    if (frequentItemsets.ContainsKey(item)) frequentItemsets[item] = frequentItemsets[item] + 1;
                    if (!frequentItemsets.ContainsKey(item)) frequentItemsets.Add(item, 1);
                }
            }
            Console.WriteLine(DictionaryToString(frequentItemsets) + "\n\n");

            //Prune - K=1
            foreach (Item item in frequentItemsets.Keys)
            {
                if (frequentItemsets[item] < minimalSupport) frequentItemsets.Remove(item);
            }
            Console.WriteLine(DictionaryToString(frequentItemsets) + "\n\n");
            Console.WriteLine("------------------------");

            Dictionary<List<Item>, int> frequentItemsets2 = new Dictionary<List<Item>, int>();
            List<List<Item>> combinations = new List<List<Item>>();
            int k = 0;
            while (true)
            {

                Console.WriteLine($"\n\n\n\nSTEP {k + 2}\n\n\n\n");
                if (k >= 1)
                {
                    List<Item> destinctItems1 = KeysDestinctToList(frequentItemsets2.Keys.ToList());
                    combinations = CreateCombinations(destinctItems1, k + 2);

                }
                else if (k == 0)
                {
                    combinations = CreateCombinations(frequentItemsets.Keys.ToList(), k + 2);
                }

                Console.WriteLine(CombinationsToString(combinations));


                frequentItemsets2 = new Dictionary<List<Item>, int>();
                frequentItemsets2 = ExecuteJoin(frequentItemsets2, combinations, transactions);

                //----------------------------PRINT-------------------------------------------------------------------------------------------------------
                foreach (var item in frequentItemsets2.Keys)
                {
                    Console.WriteLine(TransactionToString(item) + " | " + frequentItemsets2[item]);
                }
                Console.WriteLine("\n\n\n");
                //----------------------------------------------------------------------------------------------------------------------------------------


                //Step 4 - Prune - K=2
                frequentItemsets2 = ExecutePrune(frequentItemsets2, minimalSupport);
                //----------------------------PRINT-------------------------------------------------------------------------------------------------------
                foreach (var keyValuePair in frequentItemsets2)
                {
                    Console.WriteLine(TransactionToString(keyValuePair.Key) + "  |  " + frequentItemsets2[keyValuePair.Key]);
                }
                if (frequentItemsets2.Count == 0)
                {
                    return new Item("ii222");
                }
                k++;
            }
        }
        //Destinct Items------------------------------------------------------------------------------------------------------------------
        private static List<Item> KeysDestinctToList(List<List<Item>> keys)
        {
            List<Item> destinctItems = new List<Item>();
            foreach (var sets in keys)
            {
                foreach (var item in sets)
                {
                    if (!destinctItems.Contains(item)) destinctItems.Add(item);
                }
            }
            return destinctItems;
        }
        //-----------------------------------------------------------------------------------------------------------------------------------
        //Combine algorithm------------------------------------------------------------------------------------------------------------------
        static List<List<Item>> CreateCombinations(List<Item> elements, int k)
        {
            List<List<Item>> combinedList = new List<List<Item>>();
            List<Item> currentCombination = new List<Item>();

            CombineRecursive(elements, k, 0, currentCombination, combinedList);

            return combinedList;
        }

        static void CombineRecursive(List<Item> elements, int k, int start, List<Item> currentCombination, List<List<Item>> combinedList)
        {
            if (currentCombination.Count == k)
            {
                combinedList.Add(new List<Item>(currentCombination));
                return;
            }

            for (int i = start; i < elements.Count; i++)
            {
                currentCombination.Add(elements[i]);
                CombineRecursive(elements, k, i + 1, currentCombination, combinedList);
                currentCombination.RemoveAt(currentCombination.Count - 1);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------------
        //Join (Check if combination is in transaction)
        static Dictionary<List<Item>, int> ExecuteJoin(Dictionary<List<Item>, int> frequentItemsets, List<List<Item>> combinations, List<List<Item>> transactions)
        {
            foreach (List<Item> combination in combinations)
            {
                int successTimes = combination.Count;
                foreach (List<Item> transaction in transactions)
                {
                    int successCounter = 0;
                    foreach (Item combItem in combination)
                    {
                        if (transaction.Contains(combItem)) successCounter++;
                    }
                    if (successCounter == successTimes)
                    {
                        if (frequentItemsets.ContainsKey(combination)) frequentItemsets[combination] = frequentItemsets[combination] + 1;
                        if (!frequentItemsets.ContainsKey(combination)) frequentItemsets.Add(combination, 1);
                        //Console.WriteLine("Transaction\n" + TransactionToString(transaction) + "\n");
                        //Console.WriteLine("Combination\n" + TransactionToString(combination) + "\n---------\n");
                    }
                }
            }
            return frequentItemsets;
        }
        //-------------------------------------------------------------------------------------------------------------------------------------
        //Prune (Remove Set, that are lower than the minimal support
        private static Dictionary<List<Item>, int> ExecutePrune(Dictionary<List<Item>, int> frequentItemsets, float minimalSupport)
        {
            foreach (var set in frequentItemsets.Keys)
            {
                if (frequentItemsets[set] < minimalSupport) frequentItemsets.Remove(set);
            }
            return frequentItemsets;
        }
        //-------------------------------------------------------------------------------------------------------------------------------------
    }
}