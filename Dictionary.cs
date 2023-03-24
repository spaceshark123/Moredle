using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//this structure holds information for a string list to be used as the game dictionary
public class Dictionary
{
    [SerializeField]TextAsset file;
    [SerializeField]string[] dictionary;

    public Dictionary(TextAsset f) {
        file = f;
        String txt = f.text;
        //format the raw dictionary text for easier splitting
        txt = txt.Replace("\r\n", "\n");
        txt = txt.Replace("\r", "\n");
        txt = txt.Replace("%", "");
        txt = txt.Replace("!", "");
        //split the dictionary into its constituent words
        dictionary = txt.Split('\n');
    }

    public string[] GetDictionary() {
        return dictionary;
    }

    public int Length() {
        return dictionary.Length;
    }

    //uses binary search to find index of word in dictionary or returns -1 if not in dictionary
    public int IndexOf(string value) {
        int left = 0, right = dictionary.Length - 1;
        while (left <= right) {
            int mid = left + (right - left) / 2;
 
            int res = value.CompareTo(dictionary[mid]);
 
            // Check if x is present at mid
            if (res == 0)
                return mid;
 
            // If x greater, ignore left half
            if (res > 0)
                left = mid + 1;
 
            // If x is smaller, ignore right half
            else
                right = mid - 1;
        }
        //word not found
        return -1;
    }

    public bool Contains(string word) {
        return IndexOf(word) >= 0;
    }
}
