
namespace Game.Runtime
{
    public class Singleton<T> where T : new()
    {

        private static T ins;
        public static T Instance
        {
            get
            {
                if (ins == null)
                {
                    ins = new T();
                }

                return ins;
            }
        }
    }
}

