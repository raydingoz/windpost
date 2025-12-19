using System;
using System.Collections.Generic;

namespace Windpost.UI
{
    public interface IChoicePresenter
    {
        void Show(IReadOnlyList<ChoiceData> choices, Action<ChoiceData> onChoiceSelected);
        void Hide();
    }
}

