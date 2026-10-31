using System;

namespace Sexy
{
    public interface ListListener
    {
        void ListClicked(int theId, int theIdx, int theClickCount);

        void ListClosed(int theId);

        void ListHiliteChanged(int theId, int theOldIdx, int theNewIdx);
    }
}
