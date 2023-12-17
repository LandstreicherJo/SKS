﻿using SKS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Transactions;
using System.Xml;
using System.Xml.Linq;

namespace SKS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //----------------------------------------------------Assert----------------------------------------------------
            List<Item> items = new List<Item>
            {
                new Item("Milch"),
                new Item("Schokolade"),
                new Item("Nudeln"),
                new Item("Reis"),
                new Item("Brot")
            };
            Random rnd = new Random();
            List<List<Item>> transactions = new List<List<Item>>();

            //Create transactions
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

            //Create shoppingcart
            List<Item> cart1 = new List<Item>() { items[3], items[4] }; //Reis, Brot, Reis
            List<Item> cart2 = new List<Item>() { items[1], items[3], items[4] }; //Schokolade, Reis, Brot
            List<Item> cart3 = new List<Item>() { items[0], items[1] }; //Milch, Schokolade

            //----------------------------------------------------Act----------------------------------------------------
            //Console.WriteLine("-----------------------Init Object-----------------------");
            Apriori apriori = new Apriori(transactions, transactions.Count * 0.35f, 0.5f);
            var a = apriori.Associations;
            //Console.WriteLine("--------------------------Output--------------------------\n");
            Console.WriteLine(PrintConfidences(a) + "\n--------------------------------------------\n");
            //cart1
            var s = apriori.GetSuggestions(cart1);
            Console.WriteLine("Shoppingcart = " + TransactionToString(cart1));
            Console.WriteLine("Suggestions = " + TransactionToString(s) + "\n");
            //cart3
            s = apriori.GetSuggestions(cart2);
            Console.WriteLine("Shoppingcart = " + TransactionToString(cart2));
            Console.WriteLine("Suggestions = " + TransactionToString(s) + "\n");
            //cart3
            s = apriori.GetSuggestions(cart3);
            Console.WriteLine("Shoppingcart = " + TransactionToString(cart3));
            Console.WriteLine("Suggestions = " + TransactionToString(s) + "\n");
        }
        //Get suggestions-------------------------------------------------------------------------------------------------
        private static List<Item> GetSuggestions(List<Item> cart, Dictionary<List<List<Item>>, float> associations)
        {
            List<Item> suggestions = new List<Item>();
            foreach (Item item in cart)
            {
                foreach (var associationElement in associations)
                {
                    if (associationElement.Key[0].Contains(item)) suggestions.AddRange(associationElement.Key[1]);
                }
            }
            suggestions.RemoveAll(i => cart.Contains(i));
            List<Item> sug = suggestions.Distinct().ToList();
            return sug;
        }
        //---------------------------------------------------------------------------------------------------------------
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
            bool firstLoop = true;
            string output = string.Empty;
            foreach (Item item in transaction)
            {
                if (firstLoop) output += item.Name;
                if (!firstLoop) output += ", " + item.Name;
                firstLoop = false;
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

        private static string PrintConfidences(Dictionary<List<List<Item>>, float> combinations)
        {
            string output = string.Empty;
            foreach (var combination in combinations)
            {
                output += "(";
                foreach (var item in combination.Key[0])
                {
                    if (combination.Key[0].Last() != item) output += item.Name + ", ";
                    if (combination.Key[0].Last() == item) output += item.Name;
                }
                output += ") -> (";
                foreach (var item in combination.Key[1])
                {
                    if (combination.Key[1].Last() != item) output += item.Name + ", ";
                    if (combination.Key[1].Last() == item) output += item.Name;
                }
                output += ") = " + combination.Value * 100 + "%\n";
            }
            return output;
        }

        //----------------------------------------------------------------------------------------------
        private static Dictionary<List<List<Item>>, float> CreateAprioriAssociations(List<List<Item>> transactions)
        {
            float minimalSupport = transactions.Count * 0.35f;
            float confidenceLevel = 0.5f;
            Console.WriteLine("minimalSupport = " + minimalSupport + "\nconfidenceLevel = " + confidenceLevel + "\n\n\n");

            //Step 1
            Console.WriteLine($"\n\n\n\nSTEP 1\n\n\n\n");
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

                if (k >= 1)
                {
                    List<Item> destinctItems1 = KeysDestinctToList(frequentItemsets2.Keys.ToList());
                    combinations = CreateCombinations(destinctItems1, k + 2);

                }
                else if (k == 0)
                {
                    combinations = CreateCombinations(frequentItemsets.Keys.ToList(), k + 2);
                }

                if (combinations.Count != 0) Console.WriteLine($"\n\n\n\nSTEP {k + 2}\n\n\n\n");
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
                    break;
                }
                k++;
            }
            //Antimonotone Eigenschaft
            Console.WriteLine("\n\nAntimonotone Eigenschaft\n\n");

            //--------------------------Calculate Confidences--------------------------------------------------------------------------------------------------
            Console.WriteLine("\n\nCalculate confidences\n\n");
            //Step 1 - Create Dicitonary with options (Join)
            Dictionary<List<List<Item>>, float> confidences = new Dictionary<List<List<Item>>, float>();
            confidences = CreateConfidenceCombinations(combinations); //CreateConfidenceCombinations of combinations of the last step (ONLY IF 3 STEPS)
            Console.WriteLine(CombinationsToString(combinations));
            //Step 2 - CalculateConfidenceValues
            confidences = CalculateConfidenceValues(confidences, transactions);
            Console.WriteLine(PrintConfidences(confidences));
            //Step 3 - Remove KeyValuePairs under confidence level (Prune)
            confidences = PruneConfidenceValues(confidences, confidenceLevel);
            //Console.WriteLine(PrintConfidences(confidences));
            //------------------------------------------------------------------------------------------------------------------------------------------------

            return confidences;
        }


        //Calculate confidences | Combinations ------------------------------------------------------------------------------------------------------------------
        private static Dictionary<List<List<Item>>, float> CreateConfidenceCombinations(List<List<Item>> combinations)
        {
            Dictionary<List<List<Item>>, float> confidences = new Dictionary<List<List<Item>>, float>();
            foreach (List<Item> combination in combinations)
            {
                //TODO CAUSE NOW ITS HARD CODED
                //TODO What if combination [2] out of index
                confidences.Add(new List<List<Item>> { new List<Item> { combination[0] }, new List<Item> { combination[1], combination[2] } }, 0.0f); //A -> B,C
                confidences.Add(new List<List<Item>> { new List<Item> { combination[0], combination[1] }, new List<Item> { combination[2] } }, 0.0f); //A,B -> C 
                confidences.Add(new List<List<Item>> { new List<Item> { combination[2] }, new List<Item> { combination[0], combination[1] } }, 0.0f); //C -> A,B 
                confidences.Add(new List<List<Item>> { new List<Item> { combination[2], combination[0] }, new List<Item> { combination[1] } }, 0.0f); //C,A -> B 
                confidences.Add(new List<List<Item>> { new List<Item> { combination[1] }, new List<Item> { combination[2], combination[0] } }, 0.0f); //B -> C,A 
                confidences.Add(new List<List<Item>> { new List<Item> { combination[1], combination[2] }, new List<Item> { combination[0] } }, 0.0f); //B,C -> A
            }
            return confidences;
        }
        //Calculate confidences | Values
        private static Dictionary<List<List<Item>>, float> CalculateConfidenceValues(Dictionary<List<List<Item>>, float> confidences, List<List<Item>> transactions)
        {
            foreach (var confidence in confidences)
            {
                int countKeys = 0;
                int countKey0 = 0;
                float supportKeys = 0.0f;
                float supportKey0 = 0.0f;
                List<Item> allKeys = new List<Item>(confidence.Key[0]);
                allKeys.AddRange(confidence.Key[1]);
                foreach (var transaction in transactions)
                {
                    //Support (Key[0] + Key[1])                    
                    if (allKeys.All(e => transaction.Contains(e))) countKeys++;
                    //Support Key[0] <==> Count transactions.Contains => Support Key[0]
                    // In wie vielen einkäufen sind die Items des Keys vorhanden / Gesamteinkäufe
                    if (confidence.Key[0].All(e => transaction.Contains(e))) countKey0++;
                    //Calcualte and set Value for confidence
                }
                supportKeys = (float)countKeys / transactions.Count;
                supportKey0 = (float)countKey0 / transactions.Count;
                confidences[confidence.Key] = (supportKeys / supportKey0);
            }
            return confidences;
        }
        //Prune confidences Values
        private static Dictionary<List<List<Item>>, float> PruneConfidenceValues(Dictionary<List<List<Item>>, float> confidences, float confidenceLevel)
        {
            foreach (var confidence in confidences)
            {
                if (confidence.Value < confidenceLevel) confidences.Remove(confidence.Key);
            }
            return confidences;
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------
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