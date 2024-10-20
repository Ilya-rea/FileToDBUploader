using System;
namespace FileToDbUploader.Models
{
    public class Word
    {
        public string Text { get; set; }
        public int Count { get; set; }

        public Word(string text, int count)
        {
            Text = text;
            Count = count;
        }
    }
}

