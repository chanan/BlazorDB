using BlazorDB.Storage;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BlazorDB
{
    public class StorageSet<TModel> : IList<TModel> where TModel : class
    {
        private string StorageContextTypeName { get; set; } 
        private IList<TModel> List { get; set; } = new List<TModel>();
        public TModel this[int index] { get => List[index]; set => List[index] = value; }

        public int Count => List.Count;

        public bool IsReadOnly => List.IsReadOnly;

        public void Add(TModel item)
        {
            if(HasId(item))
            {
                var id = GetId(item);
                if (id > 0) throw new ArgumentException("Can't add item to set that already has an Id", "Id");
                id = SetId(item);
                Logger.ItemAddedToContext(StorageContextTypeName, item.GetType(), id, item);
            }
            else
            {
                Logger.ItemAddedToContext(StorageContextTypeName, item.GetType(), item);
            }
            List.Add(item);
        }

        //TODO: Consider using an "Id table"
        private int SetId(TModel item)
        {
            var prop = item.GetType().GetProperty("Id");
            int max = 0;
            foreach (var i in List)
            {
                int currentId = (int)prop.GetValue(i);
                if (currentId > max) max = currentId;
            }
            int id = max + 1;
            prop.SetValue(item, id);
            return id;
        }

        private int GetId(TModel item)
        {
            var prop = item.GetType().GetProperty("Id");
            return (int)prop.GetValue(item);
        }

        private bool HasId(TModel item)
        {
            var prop = item.GetType().GetProperty("Id");
            return prop != null;
        }

        public void Clear()
        {
            List.Clear();
        }

        public bool Contains(TModel item)
        {
            return List.Contains(item);
        }

        public void CopyTo(TModel[] array, int arrayIndex)
        {
            List.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TModel> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        public int IndexOf(TModel item)
        {
            return List.IndexOf(item);
        }

        public void Insert(int index, TModel item)
        {
            List.Insert(index, item);
        }

        public bool Remove(TModel item)
        {
            var removed = List.Remove(item);
            if (removed) Logger.ItemRemovedFromContext(StorageContextTypeName, item.GetType());
            return removed;
        }

        public void RemoveAt(int index)
        {
            List.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return List.GetEnumerator();
        }
    }
}
