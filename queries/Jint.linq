<Query Kind="Program">
  <NuGetReference Prerelease="true">Jint</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Jint</Namespace>
  <Namespace>Jint.Native</Namespace>
  <Namespace>Jint.Runtime</Namespace>
  <Namespace>Jint.Runtime.Interop</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>Jint.Native.Date</Namespace>
</Query>

void Main()
{
	var console = new { log = (Action<object>)((value) => Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented))) };
	var engine = new Engine(cfg =>
	{
	});

	engine
		.SetValue("console", console)
		.SetValue("node", new { type = "inject" })
		.SetValue("input", DateTime.UtcNow)
		.SetValue("output", null as object)
		//.SetValue("liquid", new Func<string, object, string>(Fluid))
		;

	try
	{
		engine.Execute(
@"{
	// Create a Date object from the payload
	console.log(input);
	let date = new Date(input);
	
	console.log(Date.now());
	console.log(new Number(input));
	console.log({ foo: new Number(input) });

	// Change the payload to be a formatted Date string
	output = date;

//let result = theFunc();
console.log(output);
}");
		engine.GetValue("output").ToObject().Dump("output");
	}
	catch (JavaScriptException ex)
	{
		$"{ex.Message} (line {ex.LineNumber})".Dump();
	}
}

//string Fluid(string source, object m)
//{
//	var model = new { Firstname = "Bill", Lastname = "Gates" };
//
//	if (FluidTemplate.TryParse(source, out var template))
//	{
//		var context = new TemplateContext();
//		context.MemberAccessStrategy.Register(model.GetType()); // Allows any public property of the model to be used
//		context.SetValue("p", model);
//		context.SetValue("m", m);
//		context.SetValue("model", m);
//		context.Model = m;
//
//		return template.Render(context);
//	}
//
//	return null;
//}

// Define other methods and classes here