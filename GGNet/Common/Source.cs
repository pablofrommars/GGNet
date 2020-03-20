using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace GGNet
{
    public class Source<T> : IDisposable
    {
        private readonly Buffer<T> buffer;
        private readonly ChannelWriter<Command> writer;

        public Source()
        {
            buffer = new Buffer<T>();

            var channel = Channel.CreateUnbounded<Command>();
            var reader = channel.Reader;
            writer = channel.Writer;

            Task.Factory.StartNew(async () =>
            {
                while (await reader.WaitToReadAsync())
                {
                    while (reader.TryRead(out Command cmd))
                    {
                        switch (cmd)
                        {
                            case _Update u:
                                break;

                            case _Set s:
                                buffer[s.Index] = s.Item;
                                break;

                            case _Add a:
                                buffer.Add(a.Item);
                                break;

                            default:
                                break;
                        }
                    }

                    await OnUpdate.Invoke();
                }
            }, TaskCreationOptions.LongRunning);
        }

        public Source(IEnumerable<T> items) : this() => Add(items);

        public int Count => buffer.Count;

        public T this[int i] => buffer[i];

        public void Add(T item) => buffer.Add(item);

        public void Add(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public event Func<Task> OnUpdate;

        internal class Command
        {
            internal T Item { get; set; }
        }

        internal class _Update : Command { }

        public ValueTask UpdateAsync(T item) => writer.WriteAsync(new _Update { Item = item });

        internal class _Set : Command 
        {
            internal int Index { get; set; }
        }

        public ValueTask SetAsync(int index, T item) => writer.WriteAsync(new _Set { Index = index, Item = item });

        internal class _Add : Command { }

        public ValueTask AddAsync(T item) => writer.WriteAsync(new _Add { Item = item });

        public void Clear() => buffer.Clear();

        public void Dispose() => writer.Complete();
    }
}
