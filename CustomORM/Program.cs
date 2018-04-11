using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CustomORM
{
    [SqlObjectName("Planes")]
    public class Plane
    {
        public int Id { get; set; }

        public string PlaneName { get; set; }

        public byte PlaneAge { get; set; }
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


    [SqlObjectName("projects")]
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


    [SqlObjectName("departments")]
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [SqlObjectName("SC")]
    public class SomeClass
    {
        [SqlObjectToModelCorresponding("departments.name")]
        public string NameDep { get; set; }

        [SqlObjectToModelCorresponding("projects.name")]
        public string Name { get; set; }
    }


    public class Program
    {
        private static void Main(string[] args)
        {
            var command = new CustomSqlCommand("DBContext");

            var testInt = 5;
            var query = command.Select(typeof(Plane), typeof(Tickets))
                    .From(typeof(Plane))
                    .Join<Plane, Tickets>((x, y) => (x.PlaneAge + testInt) < y.Price).ToString();




            // 1. != null and == null convert to IS NOT NULL and IS NULL
            // 2. add SELECT
            // 3. query in query



            var a = new CustomSqlCommand("DBContext");
            
            var b = a.Select(new List<Type> { typeof(Project), typeof(Department) }).From(new List<Type> { typeof(Project), typeof(Department) }).Where<Project, Department>((x, y) => (x.Id > 1) && (y.Name != "sdf")).AndWhere<Project, Department>((x, y) => (x.Id > 1) && (y.Name != "sdf"));
            
            Console.WriteLine(b);
            
            var c = b.ExecuteCommand<Project>();
            
            foreach (var project in c)
            {
                Console.WriteLine(project.Id + " " + project.Name);
            }
            
            ////////////////////////
            var a1 = new CustomSqlCommand("DBContext"); // need new object of command
            
            var d = a1.Select(new List<Type> { typeof(Project)}).From(new List<Type> { typeof(Project)}).Where<Project>(x => x.Id > 1 && x.Name !="dfdf");
            
            Console.WriteLine(d);
            
            var e = d.ExecuteCommand<Project>();
            
            foreach (var project in e)
            {
                Console.WriteLine(project.Id + " " + project.Name);
            }

            ////////////////////////
            var a2 = new CustomSqlCommand("DBContext"); // need new object of command

            var aa =
                a2.Select(new List<Type> {typeof (Project), typeof (Department)})
                .From(new List<Type> { typeof(Project)})
                    .Join<Project, Department>((x, y)=> x.Id == y.Id);

            Console.WriteLine(aa);

            var aaa = aa.ExecuteCommand<SomeClass>();

            foreach (var project in aaa)
            {
                Console.WriteLine(project.Name + " " + project.NameDep);
            }

            //var list = new List<int> {234, 46, 23, 56};
            //var t = new A() {Str = "sdf"};
            //int e = 1;
            //
            //var dfg = new A();
            ////Console.WriteLine(dfg.Str);
            //
            //var test =
            //    a.From(new List<Type> { typeof(C), typeof(A) }).Select(new List<Type> { typeof(C)})
            //        .Join<A, B>((x, y) => x.F1 == y.F1 && y == null)
            //        .Join<A, C>((f, n) => f.Str == list.First().ToString(), TypesOfJoin.Cross).Where<A, B>((x, y) => x.F1 > y.F1)
            //        .ToString();

            //var test =
            //    a.Join<A, B>((x, y) => x.F1 == y.F1 && y == null)
            //        .Join<A, C>((f, n) => f.Str == list.First().ToString(), TypesOfJoin.Cross)
            //        .ToString();
            //
            //Console.WriteLine(test);


            //var test = a.Join<A, C>((f, n) => f.Str == list.First().ToString(), TypesOfJoin.Cross).ToString();
            //var test = a.Join<A, C>((f, n) => list.First().ToString() == f.Str && 10 == A.M(), TypesOfJoin.Cross).ToString();

            //Console.WriteLine(test);
            //a.InnerJoin<A, B>((x,y) => (x.f1 | 1) > (y.f1 | 0) && x != null);
            //a.InnerJoin<A, B>((x, y) => (y == null) || (x.F1+ e) < y.F1 + Convert.ToInt32("5") && (y != null || x!=null) || x.Str == t.Str);
        }
    }
}