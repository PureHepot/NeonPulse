using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDataTool
{
    public class SealedValue<T>
    {
        public T DefaultValue { get; private set; }

        private T cachedValue;
        private int dirtyIndex = -1;
        private readonly List<Modification<T>> modifierGroups;

        public T Value
        {
            get
            {
                if (dirtyIndex >= 0)
                    CacheValue();

                return cachedValue;
            }
        }

        public SealedValue()
        {
            dirtyIndex = 0;
            modifierGroups = new List<Modification<T>>();
        }

        public SealedValue(T value)
        {
            DefaultValue = value;
            dirtyIndex = 0;
            modifierGroups = new List<Modification<T>>();
        }

        public void ResetDefaultValue(T value)
        {
            DefaultValue = value;
            dirtyIndex = 0;
        }

        public SealedValue<T> AddModification(Modification<T> modifierGroup)
        {
            int index = modifierGroups.Count;
            modifierGroup.setDirtyHandler = SetDirty;
            modifierGroup.index = index;
            modifierGroups.Add(modifierGroup);
            SetDirty(index);
            return this;
        }

        public bool TryGetModification<TModification>(string id, out TModification modification)
            where TModification : Modification<T>
        {
            foreach (var group in modifierGroups)
            {
                if (group.ID != id)
                    continue;

                if (group is TModification typedGroup)
                {
                    modification = typedGroup;
                    return true;
                }

                Debug.LogWarning(
                    $"[GameDataTool] Modification '{id}' is {group.GetType().Name}, not {typeof(TModification).Name}.");
                break;
            }

            Debug.LogWarning($"[GameDataTool] Modification '{id}' was not found.");
            modification = null;
            return false;
        }

        private void CacheValue()
        {
            cachedValue = dirtyIndex > 0 ? modifierGroups[dirtyIndex - 1].cachedValue : DefaultValue;
            int nextDirtyIndex = -1;

            for (int i = dirtyIndex; i < modifierGroups.Count; i++)
            {
                var group = modifierGroups[i];
                group.cachedValue = group.ModifyValue(cachedValue);
                cachedValue = group.cachedValue;

                if (group.AlwaysDirty && nextDirtyIndex < 0)
                    nextDirtyIndex = i;
            }

            dirtyIndex = nextDirtyIndex;
        }

        private void SetDirty(int index)
        {
            if (dirtyIndex >= 0)
            {
                dirtyIndex = Mathf.Min(dirtyIndex, index);
                return;
            }

            dirtyIndex = index;
        }

        public override string ToString()
        {
            object value = Value;
            return value != null ? value.ToString() : string.Empty;
        }

        public static implicit operator SealedValue<T>(T value)
        {
            return new SealedValue<T>(value);
        }

        public static implicit operator T(SealedValue<T> value)
        {
            return value.Value;
        }
    }

    public abstract class Modification<TValue>
    {
        private readonly string id;

        internal int index;
        internal TValue cachedValue;
        internal Action<int> setDirtyHandler;

        public string ID => id;
        public virtual bool AlwaysDirty => false;

        protected Modification()
        {
        }

        protected Modification(string id)
        {
            this.id = id;
        }

        internal abstract TValue ModifyValue(TValue value);

        protected virtual void SetDirty()
        {
            setDirtyHandler?.Invoke(index);
        }
    }

    public abstract class ModifierGroup<TValue, TModifier> : Modification<TValue>
        where TModifier : IEquatable<TModifier>
    {
        protected readonly List<Modifier<TModifier>> modifiers;
        protected bool alwaysDirty;

        public override bool AlwaysDirty => alwaysDirty;

        protected ModifierGroup(string id)
            : base(id)
        {
            modifiers = new List<Modifier<TModifier>>();
        }

        public abstract TModifier ModifierSum { get; }

        public void AddModifier(Modifier<TModifier> modifier)
        {
            if (modifier.releaseHandler != null)
            {
                Debug.LogError("[GameDataTool] Modifier already belongs to another group.");
                return;
            }

            modifier.setDirtyHandler = SetDirty;
            modifier.releaseHandler = RemoveModifier;
            modifiers.Add(modifier);
            CheckAlwaysDirty();
            SetDirty();
        }

        internal void RemoveModifier(Modifier<TModifier> modifier)
        {
            if (modifier.releaseHandler == null || modifier.releaseHandler.Target != this)
            {
                Debug.LogError("[GameDataTool] Tried to remove a modifier from the wrong group.");
                return;
            }

            modifier.setDirtyHandler = null;
            modifier.releaseHandler = null;
            modifiers.Remove(modifier);
            CheckAlwaysDirty();
            SetDirty();
        }

        protected override void SetDirty()
        {
            CheckAlwaysDirty();
            base.SetDirty();
        }

        private void CheckAlwaysDirty()
        {
            foreach (var modifier in modifiers)
            {
                if (!modifier.HasFunc)
                    continue;

                alwaysDirty = true;
                return;
            }

            alwaysDirty = false;
        }
    }

    public sealed class Modifier<T> where T : IEquatable<T>
    {
        private T value;
        private Func<T> getValueHandler;

        internal Action setDirtyHandler;
        internal Action<Modifier<T>> releaseHandler;

        public T Value => getValueHandler == null ? value : getValueHandler.Invoke();
        public bool HasFunc => getValueHandler != null;

        public Modifier(T value)
        {
            this.value = value;
        }

        public Modifier(Func<T> getValueHandler)
        {
            this.getValueHandler = getValueHandler;
        }

        public void SetValue(Func<T, T> setValueHandler)
        {
            var nextValue = setValueHandler(Value);
            if (nextValue.Equals(Value))
                return;

            value = nextValue;
            setDirtyHandler?.Invoke();
        }

        public void SetValue(T value)
        {
            if (value.Equals(Value))
                return;

            this.value = value;
            setDirtyHandler?.Invoke();
        }

        public void SetGetValueHandler(Func<T> getValueHandler)
        {
            this.getValueHandler = getValueHandler;
            setDirtyHandler?.Invoke();
        }

        public void Release()
        {
            releaseHandler?.Invoke(this);
        }

        public static implicit operator Modifier<T>(T value)
        {
            return new Modifier<T>(value);
        }

        public static implicit operator T(Modifier<T> value)
        {
            return value.Value;
        }
    }

    public static class SealedValueUtility
    {
        public static bool TryAddModifier<TValue, TModifier>(
            this SealedValue<TValue> sealedValue,
            string id,
            Modifier<TModifier> modifier)
            where TModifier : IEquatable<TModifier>
        {
            if (!sealedValue.TryGetModification(id, out ModifierGroup<TValue, TModifier> modifierGroup))
                return false;

            modifierGroup.AddModifier(modifier);
            return true;
        }
    }
}
