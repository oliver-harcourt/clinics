﻿using Domain.Interfaces.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClinicsDomain.UnitTests
{
    [TestClass, TestCategory("Unit")]
    public class PracticeManagersSpec
    {
        private PracticeManagers managers;

        [TestInitialize]
        public void Initialize()
        {
            this.managers = new PracticeManagers();
        }

        [TestMethod]
        public void WhenConstructed_ThenHasNoManagers()
        {
            this.managers.Managers.Should().BeEmpty();
        }

        [TestMethod]
        public void WhenAddAndManagerNotExist_ThenAddsManager()
        {
            this.managers.Add("amanagerid".ToIdentifier());

            this.managers.Managers.Count.Should().Be(1);
            this.managers.Managers[0].Should().Be("amanagerid".ToIdentifier());
        }

        [TestMethod]
        public void WhenAddAndManagerAndExist_ThenOnlySingleManager()
        {
            this.managers.Add("amanagerid".ToIdentifier());
            this.managers.Add("amanagerid".ToIdentifier());
            this.managers.Add("amanagerid".ToIdentifier());

            this.managers.Managers.Count.Should().Be(1);
            this.managers.Managers[0].Should().Be("amanagerid".ToIdentifier());
        }
    }
}