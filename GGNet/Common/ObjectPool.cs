namespace GGNet
{
    public class ObjectPool<T>
        where T : new()
    {
        private readonly Buffer<T> buffer;

        public ObjectPool(int pageCapacity = 256, int pagesIncrement = 4)
        {
            buffer = new Buffer<T>(pageCapacity, pagesIncrement);
        }

        private int index = 0;

        public void Reset() => index = 0;

        public T Get()
        {
            if (index < buffer.Count)
            {
                return buffer[index++];
            }

            var obj = new T();

            buffer.Add(obj);

            index++;

            return obj;
        }
    }
}
