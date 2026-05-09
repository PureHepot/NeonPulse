using System;
using UnityEngine;

namespace GameDataTool
{
    public class FuncGroup<T> : Modification<T>
    {
        private readonly Func<T, T> func;
        private readonly bool alwaysDirty;

        public override bool AlwaysDirty => alwaysDirty;

        public FuncGroup(string id, Func<T, T> func, bool alwaysDirty = false)
            : base(id)
        {
            this.func = func;
            this.alwaysDirty = alwaysDirty;
        }

        internal override T ModifyValue(T value)
        {
            return func == null ? default : func.Invoke(value);
        }

        public new void SetDirty()
        {
            base.SetDirty();
        }
    }

    public class FloatMultipleAddGroup : ModifierGroup<float, float>
    {
        public FloatMultipleAddGroup(string id)
            : base(id)
        {
        }

        public override float ModifierSum => Modify(0f);

        internal override float ModifyValue(float value)
        {
            return value * Modify(1f);
        }

        private float Modify(float value)
        {
            float sum = value;
            foreach (var item in modifiers)
                sum += item.Value;

            return sum;
        }
    }

    public class FloatMultipleMulGroup : ModifierGroup<float, float>
    {
        public FloatMultipleMulGroup(string id)
            : base(id)
        {
        }

        public override float ModifierSum => ModifyValue(1f);

        internal override float ModifyValue(float value)
        {
            float result = value;
            foreach (var item in modifiers)
                result *= item.Value;

            return result;
        }
    }

    public class FloatAddGroup : ModifierGroup<float, float>
    {
        public FloatAddGroup(string id)
            : base(id)
        {
        }

        public override float ModifierSum => ModifyValue(0f);

        internal override float ModifyValue(float value)
        {
            float result = value;
            foreach (var item in modifiers)
                result += item.Value;

            return result;
        }
    }

    public class FloatClampModification : Modification<float>
    {
        private float min;
        private float max;

        public float Min
        {
            get => min;
            set
            {
                min = value;
                SetDirty();
            }
        }

        public float Max
        {
            get => max;
            set
            {
                max = value;
                SetDirty();
            }
        }

        public FloatClampModification(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        internal override float ModifyValue(float value)
        {
            return Mathf.Clamp(value, min, max);
        }
    }

    public class IntMultipleAddGroup : ModifierGroup<int, float>
    {
        public IntMultipleAddGroup(string id)
            : base(id)
        {
        }

        public override float ModifierSum => Modify(0f);

        internal override int ModifyValue(int value)
        {
            return Mathf.RoundToInt(value * Modify(1f));
        }

        private float Modify(float value)
        {
            float result = value;
            foreach (var item in modifiers)
                result += item.Value;

            return result;
        }
    }

    public class IntMultipleMulGroup : ModifierGroup<int, float>
    {
        public IntMultipleMulGroup(string id)
            : base(id)
        {
        }

        public override float ModifierSum => Modify(1f);

        internal override int ModifyValue(int value)
        {
            return Mathf.RoundToInt(Modify(value));
        }

        private float Modify(float value)
        {
            float result = value;
            foreach (var item in modifiers)
                result *= item.Value;

            return result;
        }
    }

    public class IntAddGroup : ModifierGroup<int, int>
    {
        public IntAddGroup(string id)
            : base(id)
        {
        }

        public override int ModifierSum => ModifyValue(0);

        internal override int ModifyValue(int value)
        {
            int result = value;
            foreach (var item in modifiers)
                result += item.Value;

            return result;
        }
    }

    public class BoolAndGroup : ModifierGroup<bool, bool>
    {
        public bool DefaultValue { get; set; }

        public BoolAndGroup(string id, bool defaultValue)
            : base(id)
        {
            DefaultValue = defaultValue;
        }

        public override bool ModifierSum
        {
            get
            {
                if (modifiers.Count == 0)
                    return DefaultValue;

                foreach (var item in modifiers)
                {
                    if (!item)
                        return false;
                }

                return true;
            }
        }

        internal override bool ModifyValue(bool value)
        {
            return value && ModifierSum;
        }
    }

    public class BoolOrGroup : ModifierGroup<bool, bool>
    {
        public bool DefaultValue { get; set; }

        public BoolOrGroup(string id, bool defaultValue)
            : base(id)
        {
            DefaultValue = defaultValue;
        }

        public override bool ModifierSum
        {
            get
            {
                if (modifiers.Count == 0)
                    return DefaultValue;

                foreach (var item in modifiers)
                {
                    if (item)
                        return true;
                }

                return false;
            }
        }

        internal override bool ModifyValue(bool value)
        {
            return value || ModifierSum;
        }
    }
}
