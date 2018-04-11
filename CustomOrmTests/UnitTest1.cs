using CustomORM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomOrmTests
{
    [SqlObjectName("Planes")]
    public class Plane
    {
        public int Id { get; set; }

        public string PlaneName { get; set; }

        public byte PlaneAge { get; set; }

        [NotSqlObject]
        public IEnumerable<int> SomeCollection { get; set; }
    }

    public class Tickets
    {
        public int Id { get; set; }

        public int PlaneId { get; set; }

        public int AirCompanyId { get; set; }

        public string Place { get; set; }

        public int Price { get; set; }
    }

    [SqlObjectName("AirCompanies")]
    public class AirCompany
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SimpleQueryTest()
        {
            var command = new CustomSqlCommand("DBContext");

            var query = command.Select(typeof(Plane), typeof(Tickets))
                    .From(typeof(Plane))
                    .Join<Plane, Tickets>((x, y) => x.Id == y.PlaneId)
                    .Where<Plane>(x => x.Id == 2);

            var stringQuery = query.ToString();

            var passengers = query.ExecuteCommand<AirCompany>().ToList().Count();
        }


        [TestMethod]
        public void Test1()
        {
            var command = new CustomSqlCommand("DBContext");

            var testInt = 5;
            var plane = new Plane { PlaneAge = 10 };
            var query = command.Select(typeof(Plane), typeof(Tickets))
                    .From(typeof(Plane))
                    .Join<Plane, Tickets>((x, y) => (plane.PlaneAge + testInt) < y.Price).ToString();
        }


        [TestMethod]
        public void Test2()
        {
            var command = new CustomSqlCommand("DBContext");

            var list = new List<int> { 8, 9, 10 };
            command.Select(typeof(Plane))
                    .From(typeof(Plane))
                    .Where<Plane>(x => x.PlaneAge == list.Count()).ToString();
        }


        [TestMethod]
        public void Test3()
        {
            var command = new CustomSqlCommand("DBContext");

            command.Join<Plane, Tickets>((x, y) => x.PlaneAge < Convert.ToInt32("5")).ToString();
        }
    }
}
