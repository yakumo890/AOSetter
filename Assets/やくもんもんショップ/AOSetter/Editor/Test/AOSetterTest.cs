/*
Copyright (c) 2022 yakumo/yakumonmon shop
https://yakumonmon-shop.booth.pm/
This software is released under the MIT License
https://github.com/yakumo890/AOSetter/blob/master/License.txt
*/

using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Yakumo890.VRC.AOSetter.Test
{
    public class AOSetterTest
    {
        private AOSetterEngine m_engine;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_engine = new AOSetterEngine();
        }

        [Test]
        public void HasAvatarTest()
        {
            var setter = new AOSetterEngine();

            // 初期はアバターがない
            Assert.IsFalse(setter.HasAvatar());

            setter.AvatarObject = new GameObject();
            Assert.IsTrue(setter.HasAvatar());
        }

        [Test]
        public void ValidateSettingTest()
        {
            var avatar = new GameObject();
            var ao = new GameObject();

            //----- アンカーオブジェクトを新しく作らない場合 ----
            {
                var setterWithoutNewAO = new AOSetterEngine();
                setterWithoutNewAO.WillCreateNewAnchorObject = false;

                // アバターとアンカーオブジェクトが無いと無効
                Assert.IsFalse(setterWithoutNewAO.ValidateSetting());

                setterWithoutNewAO.AvatarObject = avatar;
                Assert.IsFalse(setterWithoutNewAO.ValidateSetting());

                setterWithoutNewAO.AvatarObject = null;
                setterWithoutNewAO.AnchorObject = ao;
                Assert.IsFalse(setterWithoutNewAO.ValidateSetting());

                setterWithoutNewAO.AvatarObject = avatar;
                Assert.IsTrue(setterWithoutNewAO.ValidateSetting());
            }

            //----- アンカーオブジェクトを新しく作る場合 ----
            {
                var setterWithNewAO = new AOSetterEngine();
                setterWithNewAO.WillCreateNewAnchorObject = true;

                // アバターとアンカーオブジェクトの親が無いと無効
                Assert.IsFalse(setterWithNewAO.ValidateSetting());

                // アバターを設定すると自動で親が設定される
                setterWithNewAO.AvatarObject = avatar;
                Assert.IsTrue(setterWithNewAO.ValidateSetting());

                //親がいないと無効
                setterWithNewAO.NewAnchorObjectParent = null;
                Assert.IsFalse(setterWithNewAO.ValidateSetting());

                //アバターがないと無効
                setterWithNewAO.NewAnchorObjectParent = new GameObject();
                setterWithNewAO.AvatarObject = null;
                Assert.IsFalse(setterWithNewAO.ValidateSetting());

                setterWithNewAO.AnchorObject = setterWithNewAO.AvatarObject;
                //オブジェクトの名前が無いと無効
                setterWithNewAO.NewAnchorObjectName = "";
                Assert.IsFalse(setterWithNewAO.ValidateSetting());
                setterWithNewAO.NewAnchorObjectName = null;
                Assert.IsFalse(setterWithNewAO.ValidateSetting());
            }
        }

        [Test]
        public void InitializeNewAnchorObjectParentTest()
        {
            var avatar = new GameObject();

            // ----アンカーオブジェクトの親がない場合、アバター自体が設定される            
            {
                var setterWithoutParent = new AOSetterEngine();
                setterWithoutParent.AvatarObject = avatar;
                Assert.AreEqual(avatar, setterWithoutParent.NewAnchorObjectParent);
            }

            var aoParent = new GameObject();

            // ---アンカーオブジェクトがすでにある場合、変更しない
            {
                var setterWithParent = new AOSetterEngine();
                setterWithParent.NewAnchorObjectParent = aoParent;
                setterWithParent.AvatarObject = avatar;
                Assert.AreEqual(aoParent, setterWithParent.NewAnchorObjectParent);
            }
        }

        [Test]
        public void LoadRenderersTest()
        {
            var setter = new AOSetterEngine();

            var avatar = CreateTestAvatar();
            setter.AvatarObject = avatar;

            var expected = CountRenderer(avatar);

            Assert.AreEqual(expected, setter.Renderers.Count);
        }

        [Test]
        public void SetAnchorOverrideTest()
        {
            // ----すでにあるアンカーオブジェクトを設定する場合----
            {
                var setterWithAO = new AOSetterEngine();
                setterWithAO.WillCreateNewAnchorObject = false;

                var avatar = CreateTestAvatar();

                //アバターがないので失敗する
                Assert.IsFalse(setterWithAO.SetAnchorOverride());

                setterWithAO.AvatarObject = avatar;

                //アンカーオブジェクトが無いので失敗する
                Assert.IsFalse(setterWithAO.SetAnchorOverride());

                //アンカーオブジェクトはあるが、アバターがないので失敗する
                setterWithAO.AvatarObject = null;
                Assert.IsFalse(setterWithAO.SetAnchorOverride());

                setterWithAO.AvatarObject = avatar;

                var ao = new GameObject();
                ao.name = "AO";
                ao.transform.SetParent(avatar.transform);
                setterWithAO.AnchorObject = ao;

                Assert.IsTrue(setterWithAO.SetAnchorOverride());

                //アンカーオブジェクトが設定されているか
                Assert.IsTrue(CheckAnchorOverride(avatar, ao));
            }

            // ----新規にアンカーオブジェクトを作って設定する場合----
            {
                const string aoObjectName = "AO";

                var setterWithNewAO = new AOSetterEngine();
                setterWithNewAO.WillCreateNewAnchorObject = true;

                var avatar = CreateTestAvatar();
                setterWithNewAO.AvatarObject = avatar;

                //名前も親がなければ失敗
                setterWithNewAO.NewAnchorObjectName = null;
                setterWithNewAO.NewAnchorObjectParent = null;
                
                Assert.IsFalse(setterWithNewAO.SetAnchorOverride());

                // 名前があっても親がなければ失敗
                setterWithNewAO.NewAnchorObjectName = aoObjectName;
                Assert.IsFalse(setterWithNewAO.SetAnchorOverride());

                // 親がいても名前がなければ失敗
                setterWithNewAO.NewAnchorObjectParent = setterWithNewAO.AvatarObject;
                setterWithNewAO.NewAnchorObjectName = "";
                Assert.IsFalse(setterWithNewAO.SetAnchorOverride());
                setterWithNewAO.NewAnchorObjectName = null;
                Assert.IsFalse(setterWithNewAO.SetAnchorOverride());

                setterWithNewAO.NewAnchorObjectName = aoObjectName;
                setterWithNewAO.NewAnchorObjectVector = new Vector3(1, 2, 3);                
                
                Assert.IsTrue(setterWithNewAO.SetAnchorOverride());

                //アンカーオブジェクトが作られているか
                var ao = avatar.transform.Find(aoObjectName);
                Assert.IsNotNull(ao);
                Assert.AreEqual(new Vector3(1, 2, 3), ao.transform.position);

                Assert.IsTrue(CheckAnchorOverride(avatar, ao.gameObject));
            }

        }

        private GameObject CreateTestAvatar()
        {
            var avatar = new GameObject();

            var mesh1 = new GameObject();
            mesh1.AddComponent<MeshRenderer>();
            mesh1.transform.SetParent(avatar.transform);

            var skinnedMesh = new GameObject();
            skinnedMesh.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.transform.SetParent(avatar.transform);

            var mesh2 = new GameObject();
            mesh2.AddComponent<MeshRenderer>();
            mesh2.transform.SetParent(mesh1.transform);

            return avatar;
        }

        private int CountRenderer(GameObject avatar)
        {
            var renderers = GetRenderers(avatar);
            return renderers.Count;
        }

        private bool CheckAnchorOverride(GameObject avatar, GameObject ao)
        {
            bool result = true;

            var renderers = GetRenderers(avatar);
            foreach (var renderer in renderers)
            {
                result &= renderer.probeAnchor == ao.transform;
            }

            return result;
        }

        private List<Renderer> GetRenderers(GameObject avatar)
        {
            var renderers = new List<Renderer>();

            renderers.AddRange(avatar.GetComponentsInChildren<MeshRenderer>());
            renderers.AddRange(avatar.GetComponentsInChildren<SkinnedMeshRenderer>());

            return renderers;
        }
    }
}