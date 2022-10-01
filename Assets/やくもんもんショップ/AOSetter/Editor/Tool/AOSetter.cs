/*
Copyright (c) 2022 yakumo/yakumonmon shop
https://yakumonmon-shop.booth.pm/
This software is released under the MIT License
https://github.com/yakumo890/AOSetter/blob/master/License.txt
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Yakumo890.VRC.AOSetter
{
    /// <summary>
    /// AOSetterのUI
    /// </summary>
    public class AOSetter : EditorWindow
    {
        private static AOSetterEngine m_engine = new AOSetterEngine();

        private bool m_enableButton; // 一括設定のボタンを表示するか
        private bool m_showIndivisualSettings; //個別設定を表示するか
        private Vector2 m_scrollPosition; //個別設定のスクロール

        [MenuItem("Yakumo890/AOSetter")]
        static void ShowWindow()
        {
            var window = GetWindow<AOSetter>();
            window.titleContent = new GUIContent("AO Setter");

            EditorApplication.hierarchyChanged += OnChanged;
        }

        private void OnGUI()
        {
            GUILayout.Label("アバターのAO(Anchor Override)を一括で設定する", EditorStyles.boldLabel);
            GUILayout.Space(20);

            m_engine.AvatarObject = EditorGUILayout.ObjectField("対象のアバター", m_engine.AvatarObject, typeof(GameObject), true) as GameObject;

            if (!m_engine.HasAvatar())
            {
                return;
            }

            m_enableButton = m_engine.ValidateSetting();

            GUILayout.Space(15);
            EditorGUI.BeginChangeCheck();
            m_engine.WillCreateNewAnchorObject = GUILayout.Toggle(m_engine.WillCreateNewAnchorObject, "新規にアンカーオブジェクトを作成する");
            if (EditorGUI.EndChangeCheck())
            {
                m_enableButton = m_engine.ValidateSetting();
            }

            if (m_engine.WillCreateNewAnchorObject)
            {
                EditorGUI.BeginChangeCheck();
                m_engine.NewAnchorObjectName = EditorGUILayout.TextField("アンカーオブジェクトの名前", m_engine.NewAnchorObjectName);
                if (EditorGUI.EndChangeCheck())
                {
                    m_enableButton = m_engine.ValidateSetting();
                }

                m_engine.NewAnchorObjectVector = EditorGUILayout.Vector3Field("アンカーオブジェクトのポジション(ローカル座標)", m_engine.NewAnchorObjectVector);

                EditorGUI.BeginChangeCheck();
                m_engine.NewAnchorObjectParent = EditorGUILayout.ObjectField(
                    "アンカーオブジェクトの親",
                    m_engine.NewAnchorObjectParent,
                    typeof(GameObject),
                    true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                {
                    m_enableButton = m_engine.ValidateSetting();
                }

            }
            else
            {
                EditorGUI.BeginChangeCheck();
                m_engine.AnchorObject = EditorGUILayout.ObjectField("アンカーオブジェクト", m_engine.AnchorObject, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                {
                    m_enableButton = m_engine.ValidateSetting();
                }
            }

            GUILayout.Space(15);
            EditorGUI.BeginDisabledGroup(!m_enableButton);
            if (GUILayout.Button("AOを一括で設定"))
            {
                var result = m_engine.SetAnchorOverride();
                if (!result)
                {
                    Debug.LogError("[AOSetter] AnchorOverrideのセットに失敗しました。設定項目を確認してください");
                    EditorGUILayout.HelpBox("AnchorOverrideセットに失敗しました。設定項目を確認してください", MessageType.Error, true);
                }
                else
                {
                    Debug.Log("[AOSetter] AnchorOverrideセット完了");
                }
            }
            EditorGUI.EndDisabledGroup();

            m_showIndivisualSettings = EditorGUILayout.Foldout(m_showIndivisualSettings, "個別設定");
            if (m_showIndivisualSettings)
            {
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);


                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("メッシュ(オブジェクト名)", EditorStyles.boldLabel);
                foreach (var renderer in m_engine.Renderers)
                {
                    EditorGUILayout.LabelField(renderer.name);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("アンカーオブジェクト", EditorStyles.boldLabel);
                for (int i = 0; i < m_engine.Renderers.Count; ++i)
                {
                    m_engine[i] = EditorGUILayout.ObjectField(m_engine[i], typeof(GameObject), true) as GameObject;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// ヒエラルキーに変更があった場合の処理<br/>
        /// レンダラーを取得し直す
        /// </summary>
        private static void OnChanged()
        {
            m_engine.LoadRenderers();
        }
    }

    /// <summary>
    /// AOSetterのエンジン
    /// </summary>
    public class AOSetterEngine
    {
        private GameObject m_avatarObject;
        /// <value>対象のアバターオブジェクト</value>
        public GameObject AvatarObject
        {
            get
            {
                return m_avatarObject;
            }
            set
            {
                m_avatarObject = value;
                LoadRenderers();
                InitializeNewAnchorObjectParent();
            }
        }

        /// <value>設定予定のアンカーオブジェクト</value>
        public GameObject AnchorObject
        {
            get;
            set;
        } = null;

        /// <value>アンカーオブジェクトを新規に作成するか</value>
        public bool WillCreateNewAnchorObject
        {
            get;
            set;
        } = false;

        /// <value>新規にアンカーオブジェクトの名前</value>
        public string NewAnchorObjectName
        {
            get;
            set;
        } = "AnchorTarget";
        
        /// <value>新規のアンカーオブジェクトのposition</value>
        public Vector3 NewAnchorObjectVector
        {
            get;
            set;
        } = new Vector3();

        /// <value>新規のアンカーオブジェクトの親</value>
        public GameObject NewAnchorObjectParent
        {
            get;
            set;
        } = null;

        /// <value>アバターのレンダラー一覧</value>        
        public List<Renderer> Renderers
        {
            get;
            private set;
        } = new List<Renderer>();

        public AOSetterEngine()
        {
            AvatarObject = null;
        }

        /// <summary>
        /// Rendererに配列のようにアクセスできる
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>指定したインデックスにあるRendererのアンカーオブジェクト</returns>
        public GameObject this[int index]
        {
            get
            {
                return Renderers[index].probeAnchor?.gameObject;
            }
            set
            {
                Renderers[index].probeAnchor = value?.transform;
            }
        }

        /// <summary>
        /// アバターがセットされているか
        /// </summary>
        /// <returns>true: セットされている<br/>false: セットされていない(null)</returns>
        public bool HasAvatar()
        {
            return AvatarObject != null;
        }

        /// <summary>
        /// AOをセットするための設定が正常に行われているか
        /// </summary>
        /// <returns>true: 正常<br/>false: 正常でない</returns>
        public bool ValidateSetting()
        {
            if (!HasAvatar())
            {
                return false;
            }

            if (WillCreateNewAnchorObject)
            {
                if (NewAnchorObjectName == null || NewAnchorObjectName == "")
                {
                    return false;
                }
                return NewAnchorObjectParent != null;               
            }
            else
            {
                return AnchorObject != null;
            }
        }

        /// <summary>
        /// アンカーオブジェクトをレンダラーのアンカーオーバーライドにセットする
        /// </summary>
        /// <returns>true: セット完了<br/>false: セット失敗</returns>
        public bool SetAnchorOverride()
        {
            if (!HasAvatar())
            {
                return false;
            }

            GameObject ao = GetAnchorObject();
            if (ao == null)
            {
                return false;
            }

            LoadRenderers();
            foreach (var renderer in Renderers)
            {
                renderer.probeAnchor = ao.transform;
            }

            return true;
        }

        /// <summary>
        /// アバターのレンダラーを取得する
        /// 対象はMeshRendererとSkinnedMeshRenderer
        /// </summary>
        public void LoadRenderers()
        {
            if (!HasAvatar())
            {
                return;
            }

            Renderers = new List<Renderer>();
            Renderers.AddRange(AvatarObject.GetComponentsInChildren<MeshRenderer>());
            Renderers.AddRange(AvatarObject.GetComponentsInChildren<SkinnedMeshRenderer>());
        }

        /// <summary>
        /// アンカーオブジェクトの親オブジェクトにアバターのオブジェクトを設定する<br/>
        /// すでに親オブジェクトが設定されている場合はなにもしない
        /// </summary>
        private void InitializeNewAnchorObjectParent()
        {
            if (NewAnchorObjectParent != null)
            {
                return;
            }
            
            NewAnchorObjectParent = AvatarObject;
        }

        /// <summary>
        /// 設定するべきアンカーオブジェクトを取得する
        /// </summary>
        /// <returns>アンカーオブジェクト</returns>
        private GameObject GetAnchorObject()
        {
            GameObject ao = null;
            if (WillCreateNewAnchorObject)
            {
                ao = CreateNewAnchorObject();
            }
            else
            {
                ao = AnchorObject;
            }

            return ao;
        }

        /// <summary>
        /// アンカーオブジェクトを新規に作成する
        /// </summary>
        /// <returns>作成したアンカーオブジェクト</returns>
        private GameObject CreateNewAnchorObject()
        {
            if (NewAnchorObjectName == null || NewAnchorObjectName.Length == 0)
            {
                return null;
            }

            if (NewAnchorObjectParent == null)
            {
                return null;
            }

            GameObject ao = new GameObject();
            ao.name = NewAnchorObjectName;
            ao.transform.SetParent(NewAnchorObjectParent.transform);
            ao.transform.localPosition = NewAnchorObjectVector;

            return ao;
        }
    }
}