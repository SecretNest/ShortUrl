using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace SecretNest.ShortUrl
{
	public abstract class OtherResult
	{
		public abstract bool HasContent { get; }
		public abstract int StatusCode { get; }
		public abstract string Context { get; }
		public abstract string ContentType { get; }
	}

	public abstract class NoContentResult : OtherResult
	{
		public override bool HasContent => false;
		public override string Context => throw new NotSupportedException();
		public override string ContentType => throw new NotSupportedException();
	}

	public class Status204Result : NoContentResult
	{
		public override int StatusCode => 204;
	}

	public class Status205Result : NoContentResult
	{
		public override int StatusCode => 205;
	}

	public class Status406Result : NoContentResult
	{
		public override int StatusCode => 406;
	}

	public class Status409Result : NoContentResult
	{
		public override int StatusCode => 409;
	}

	public class Status410Result : NoContentResult
	{
		public override int StatusCode => 410;
	}

	public class Status500Result : NoContentResult
	{
		public override int StatusCode => 500;
	}

	public abstract class ContentResult : OtherResult
	{
		public override bool HasContent => true;
	}

	public class Status200Result : ContentResult
	{
		public override int StatusCode => 200;
		public override string Context => context;
		public override string ContentType => contentType;

		private readonly string context;
		private readonly string contentType;

		protected Status200Result() { }
		public Status200Result(string context, string contentType)
		{
			this.context = context;
			this.contentType = contentType;
		}
	}

	public class Status200Result<T> : Status200Result
	{
		public override string Context => JsonConvert.SerializeObject(context, new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			}
		});
		public override string ContentType => "application/json";

		readonly T context;

		public Status200Result(T context)
		{
			this.context = context;
		}
	}

}
