namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
    using GameCreator.Variables;

    [AddComponentMenu("")]
	public class ConditionDistance : ICondition
	{
        public enum Compare
        {
            Equals,
            IsDifferent,
            GreaterThan,
            LessThan
        }

        public TargetGameObject objectA = new TargetGameObject(TargetGameObject.Target.Invoker);

        [Space]
        public TargetGameObject objectB = new TargetGameObject(TargetGameObject.Target.GameObject);

        [Space]
        public Compare compare = Compare.Equals;
        public NumberProperty distance = new NumberProperty(5f);

        public override bool Check(GameObject target)
		{
            Transform a = this.objectA.GetTransform(target);
            Transform b = this.objectB.GetTransform(target);

            if (a == null || b == null) return false;

            float userDistance = this.distance.GetValue(target);
            float realDistance = Vector3.Distance(a.position, b.position);

            switch (this.compare)
            {
                case Compare.Equals: return Mathf.Approximately(userDistance, realDistance);
                case Compare.IsDifferent: return !Mathf.Approximately(userDistance, realDistance);
                case Compare.GreaterThan: return realDistance > userDistance;
                case Compare.LessThan: return realDistance < userDistance;
            }

            return false;
        }

        #if UNITY_EDITOR
        public static new string NAME = "Object/Distance";
        private const string NODE_TITLE = "Distance between {0} and {1} {2} {3}";

        public override string GetNodeTitle()
        {
            return string.Format(
                NODE_TITLE,
                this.objectA,
                this.objectB,
                this.compare,
                this.distance
            );
        }

        #endif
    }
}
