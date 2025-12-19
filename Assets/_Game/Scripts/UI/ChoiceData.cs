using System;

namespace Windpost.UI
{
    [Serializable]
    public struct ChoiceData
    {
        public string Id;
        public string Text;

        public ChoiceData(string id, string text)
        {
            Id = id;
            Text = text;
        }
    }
}

