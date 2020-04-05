<Query Kind="Program">
  <NuGetReference Prerelease="true">Fluid.Core</NuGetReference>
  <NuGetReference>Jint</NuGetReference>
  <Namespace>Jint</Namespace>
  <Namespace>Fluid</Namespace>
  <Namespace>Fluid.Values</Namespace>
  <Namespace>Jint.Native</Namespace>
</Query>

void Main()
{
	var engine = new Engine()
			.SetValue("log", new Action<object>(Console.WriteLine))
			.SetValue("liquid", new Func<string, object, string>(Fluid))
			;

	engine.Execute(@"
	      function hello() { 
		  	var m = { 'hello': 'Hello', 'hi': 'Hi' };
	        log(liquid('{{ m.hi }} {{ model.hello }} {{ hello }} {{ p.Firstname }} {{ p.Lastname }}', m));
	      };
	      
	      hello();
	    ");
}

string Fluid(string source, object m)
{
	var model = new { Firstname = "Bill", Lastname = "Gates" };

	if (FluidTemplate.TryParse(source, out var template))
	{
		var context = new TemplateContext();
		context.MemberAccessStrategy.Register(model.GetType()); // Allows any public property of the model to be used
		context.SetValue("p", model);
		context.SetValue("m", m);
		context.SetValue("model",  m);
		context.Model = m;

		return template.Render(context);
	}

	return null;
}

// Define other methods and classes here

