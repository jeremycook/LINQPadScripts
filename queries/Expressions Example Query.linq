<Query Kind="Program" />

void Main()
{
	var localManagers = new List<ManagerialEmployee>();
	var Managers = Enumerable.Empty<ManagerialEmployee>().AsQueryable();

	var query =
		//from (select DocumentId, Document from Documents where Type = 'ManagerialEmployee') m
		from m in Managers
		//where
		// 	(select count(*) from ISalariedEmployee e where e._DocumentId = m.DocumentId and (e.Salary < @c1)) < @c2 and
		//	m.Salary > @c3 and
		//	m.DocumentId in (@c4_localManagers, @c5_localManagers, @c6_localManagers)
		where 
			m.Employees.OfType<ISalariedEmployee>().Where(e => 
				e.Salary < 50000
			).Count() < 10 && 
			m.Salary > 100000 &&
			localManagers.Contains(m)
		//orderby (select count(*) from IEmployee _1 where _1._DocumentId = m.DocumentId
		orderby m.Employees.Count()
		//select m.*
		select m;
		//@c1 = 50000
		//@c2 = 10
		//@c3 = 100000
		//@c4_localManagers=3, @c5_localManagers=1, @c6_localManagers=9

	query.Expression.Dump();
}

// Define other methods and classes here

public interface IPerson
{
	string FirstName { get; set; }
	string LastName { get; set; }
}

public interface IEmployee : IPerson
{
	string Title { get; set; }

	IManager Manager { get; set; }
}

public interface ISalariedEmployee : IEmployee
{
	decimal Salary { get; set; }
}

public interface IHourlyEmployee : IEmployee
{
	decimal HourlyWage { get; set; }
}

public interface IManager : IEmployee
{
	List<IEmployee> Employees { get; set; }
}

public class SalariedEmployee : ISalariedEmployee
{
	public string FirstName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public string LastName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public IManager Manager { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public decimal Salary { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}

public class HourlyEmployee : IHourlyEmployee
{
	public string FirstName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public string LastName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public IManager Manager { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public decimal HourlyWage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}

public class ManagerialEmployee : IManager, ISalariedEmployee
{
	public string FirstName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public string LastName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public IManager Manager { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public List<IEmployee> Employees { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public decimal Salary { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}