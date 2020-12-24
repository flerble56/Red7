﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Red_7_v2._0
{
    public class Hand
    {
        public List<Card> hand;
        protected int size;

        public Hand()
        {
            hand = new List<Card>();
        }
        public int Size { get { return size; } }
        public Card GetCard(int index)
        {
            if (index > hand.Count)
            {
                return new Card(0, 0);
            }
            else
            {
                return hand[index];
            }
        }
        public int FindCard(Card card)
        {
            for (int i = 0; i < size; i++)
            {
                Card c = hand[i];
                if (c.GetScore() == card.GetScore())
                {
                    return i;
                }
            }
            return -1;
        }
        public void AddCard(int r, int c)
        {
            hand.Add(new Card(r, c));
            size++;
        }
        public void RemoveCard(Card c)
        {
           RemoveCardByIndex(FindCard(c));
        }
        public Card RemoveCardByIndex(int index)
        {
            Card c = GetCard(index);
            hand.RemoveAt(index);
            size--;
            return c;
        }
    }
}
