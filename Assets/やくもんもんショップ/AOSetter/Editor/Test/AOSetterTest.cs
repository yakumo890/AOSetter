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

                setterWithNewAO.NewAnchorObjectParent = null;
                Assert.IsFalse(setterWithNewAO.ValidateSetting());

                setterWithNewAO.NewAnchorObjectParent = new GameObject();
                setterWithNewAO.AvatarObject = null;
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
                var avatar = CreateTestAvatar();
                setterWithAO.AvatarObject = avatar;

                var ao = new GameObject();
                ao.name = "AO";
                ao.transform.SetParent(avatar.transform);
                setterWithAO.AnchorObject = ao;

                setterWithAO.SetAnchorOverride();
                Assert.IsTrue(CheckAnchorOverride(avatar, ao));
            }

            // ----新規にアンカーオブジェクトを作って設定する場合----
            {
                var setterWithNewAO = new AOSetterEngine();
                var avatar = CreateTestAvatar();
                setterWithNewAO.AvatarObject = avatar;

                setterWithNewAO.WillCreateNewAnchorObject = true;
                setterWithNewAO.NewAnchorObjectVector = new Vector3(1, 2, 3);
                setterWithNewAO.NewAnchorObjectName = "AO";

                setterWithNewAO.SetAnchorOverride();

                var ao = avatar.transform.Find("AO");
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