using BlazorDB.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BlazorDB
{
    public class StorageSet<TModel> : IList<TModel> where TModel : class
    {
        private string StorageContextTypeName { get; set; }
        private IList<TModel> List { get; set; } = new List<TModel>();

        public TModel this[int index]
        {
            get => List[index];
            set => List[index] = value;
        }

        public int Count => List.Count;

        public bool IsReadOnly => List.IsReadOnly;

        public void Add(TModel item)
        {
            if (item == null) throw new ArgumentException("Can't add null");
            if (HasId(item))
            {
                var id = GetId(item);
                if (id > 0) throw new ArgumentException("Can't add item to set that already has an Id", "Id");
                Logger.ItemAddedToContext(StorageContextTypeName, item.GetType(), item);
            }
            else
            {
                throw new ArgumentException("Model must have Id property");
            }

            List.Add(item);
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
            if (item == null) throw new ArgumentException("Can't remove null");
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

        //TODO: Consider using an "Id table"
        private int SetId(TModel item)
        {
            var prop = item.GetType().GetProperty("Id");
            if (prop == null) throw new ArgumentException("Model must have an Id property");
            var max = List.Select(i => (int) prop.GetValue(i)).Concat(new[] {0}).Max();

            var id = max + 1;
            prop.SetValue(item, id);
            return id;
        }

        private static int GetId(TModel item)
        {
            var prop = item.GetType().GetProperty("Id");
            if (prop == null) throw new ArgumentException("Model must have an Id property");
            return (int) prop.GetValue(item);
        }

        private static bool HasId(TModel item)
        {
            var prop = item.GetType().GetProperty("Id");
            return prop != null;
        }
    }
}