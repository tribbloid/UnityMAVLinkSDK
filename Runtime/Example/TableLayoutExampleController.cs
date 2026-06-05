using System.Collections.Generic;
using MAVLinkSDK.UI.Tables;
using UnityEngine;

namespace MAVLinkSDK.Example
{
    public class TableLayoutExampleController : MonoBehaviour
    {
        public List<TableLayout> Examples = new();

        public void ShowExample(TableLayout example)
        {
            Examples.ForEach(t =>
            {
                if (t != example) t.gameObject.SetActive(false);
            });

            if (!example.gameObject.activeInHierarchy) example.gameObject.SetActive(true);
        }
    }
}