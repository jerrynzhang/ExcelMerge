using System;

namespace ExcelMerge.GUI.Views
{
    enum TargetType
    {
        All,
        First,
    }

    class DiffViewEventArgs<T> : EventArgs
    {
        public T Sender { get; }
        public IServiceProvider Container { get; }
        public TargetType TargetType { get; }

        public DiffViewEventArgs(T sender, IServiceProvider container, TargetType targetType = TargetType.All)
        {
            Sender = sender;
            Container = container;
            TargetType = targetType;
        }
    }
}
