using System;
using System.Collections;
using System.Collections.Generic;


namespace Loadings
{
    public interface ILoading
    {
        void Load();
        bool IsLoaded { get; }
    }

    public abstract class LoadingList
    {
        protected List<ILoading> loadings = new List<ILoading>();

        public void AddStep(Action init, Func<bool> isLoad)
        {
            AddStep(new Initializer(init, isLoad));
        }
        public void AddStep(Action init)
        {
            AddStep(new Initializer(init));
        }
        public void AddStep(ILoading loading)
        {
            loadings.Add(loading);
        }
    }

    public class LoadingInTurn : LoadingList
    {
        public bool IsLoaded { get; set; }

        public void Load()
        {
            GameMain.Instance.StartCoroutine(LoadInTurn());
        }

        public IEnumerator LoadInTurn()
        {
            var initAssets = loadings.GetEnumerator();
            while (initAssets.MoveNext())
            {
                yield return Pool.WaitForEndOfFrame;
                var asset = initAssets.Current;
                asset.Load();
                while (!asset.IsLoaded)
                {
                    yield return Pool.WaitForEndOfFrame;
                }
            }
            IsLoaded = true;
        }
    }

    public class LoadingAsync : LoadingList
    {
        public bool IsLoaded
        {
            get
            {
                for (int i = 0; i < loadings.Count; i++)
                {
                    if (!loadings[i].IsLoaded)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public void Load()
        {
            //TO DO : create is realy async
            for (int i = 0; i < loadings.Count; i++)
            {
                loadings[i].Load();
            }
        }
    }

    public class Initializer : ILoading
    {
        private readonly Action initializedAction;
        private readonly Func<bool> isLoadAction;

        public Initializer(Action init)
        {
            initializedAction = init;
            isLoadAction = () => true;
        }
        public Initializer(Action init, Func<bool> isLoaded)
        {
            initializedAction = init;
            isLoadAction = isLoaded;
        }

        public bool IsLoaded => isLoadAction();

        public void Load()
        {
            initializedAction?.Invoke();
        }
    }
}