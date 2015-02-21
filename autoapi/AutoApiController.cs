using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json.Linq;
using zeco.autoapi.Extensions;

namespace zeco.autoapi
{

    public abstract class ApiControllerBase : ApiController
    {
        internal static readonly string
            SourcePropertyName,
            SourceIdPropertyName,
            JsSourceIdPropertyName;

        static ApiControllerBase()
        {
            SourcePropertyName = ObjectExtensions.NameOf((Item<object> t) => t.Source);
            SourceIdPropertyName = ObjectExtensions.NameOf((Item<object> t) => t.SourceId);
            JsSourceIdPropertyName = SourceIdPropertyName.Decapitalize();
        }

    }

    public abstract class ApiControllerBase<TContext, TUser> : ApiControllerBase
        where TContext : AutoApiDbContext<TUser>
        where TUser : AutoApiUser
    {
        public TContext Context { get; set; }

        public AutoApiUserManager<TUser> UserManager { get; set; }

        public AutoApiUserStore<TUser> UserStore { get; set; }

        private TUser _user;

        protected virtual TUser Self
        {
            get
            {
                if (_user == null)
                {
                    var userId = Guid.Parse(User.Identity.GetUserId());
                    _user = UserManager.FindById(userId);
                }
                return _user;
            }
        }

        //internal ApiControllerBase() { }
    }

    [Authorize]
    public abstract class AutoApiController<T, TContext, TUser> : ApiControllerBase<TContext, TUser> 
        where T : class, IIdentifiable, new() where TUser : AutoApiUser where TContext : AutoApiDbContext<TUser>
    {

        private bool _batching;

        #region API

        [HttpGet]
        public virtual T Get(Guid id)
        {
            return Get().SingleOrDefault(item => item.Id == id);
        }

        [HttpGet]
        public virtual IQueryable<T> Get()
        {
            if (Self == null)
                return (new T[0]).AsQueryable();

            if (Self.IsAdmin)
                return Context.Query<T>();

            if (IsRootedIn<TUser>())
                return FilterBy(Self, u => u.Id);

            return new T[0].AsQueryable();
        }

        [HttpPatch]
        public virtual IQueryable<T> Patch(dynamic payload)
        {
            var array = ((JArray) payload)
                .Select(Convert.ToString)
                .Select(Guid.Parse)
                .ToArray();

            if (array.Length == 0)
                return new T[0].AsQueryable();

            return Get().Where(entity => array.Contains(entity.Id));
        }

        [HttpPost]
        public virtual T Post(dynamic payload)
        {
            var id = Guid.Parse((string) payload.id);

            if (CanEditItem(id, IsRootedIn(Self, id)))
            {
                var item = Get(id);
                TranscribeFromPayload(item, payload);
                Context.SaveChanges();
                return item;
            }

            throw new HttpResponseException(HttpStatusCode.Forbidden);

        }

        [HttpPut]
        public virtual object Put(dynamic payload)
        {

            var array = payload as JArray;
            if (array != null)
            {
                _batching = true;
                var list = new List<object>();
                foreach (dynamic item in array)
                    list.Add(Put(item));

                _batching = false;
                Context.SaveChanges();
                return list.ToArray();
            }

            Guid? sourceId = null;
            if (((IDictionary<string, JToken>) payload).ContainsKey(JsSourceIdPropertyName))
                sourceId = Guid.Parse((string) payload.sourceId);

            if (CanAddItem(sourceId))
            {
                var item = new T();

                if (sourceId.HasValue)
                    item.GetType()
                        .GetProperty(SourceIdPropertyName)
                        .SetValue(item, sourceId.Value);

                TranscribeFromPayload(item, payload);
                return Put(item);
            }

            throw new HttpResponseException(HttpStatusCode.Forbidden);
        }

        protected virtual T Put(T item)
        {
            Context.Set<T>().Add(item);
            if (!_batching) Context.SaveChanges();
            return item;
        }

        [HttpDelete]
        public virtual bool Delete(Guid id)
        {
            if (CanDeleteItem(id))
            {
                var item = Get(id);
                if (item != null)
                {
                    item.IsDeleted = true;
                    Context.SaveChanges();
                }
                return true;
            }

            return false;
        }

        #endregion

        #region Protected

        protected virtual bool CanEditItem(Guid id, bool owned)
        {
            if (Self.IsAdmin)
                return true;

            if (typeof (T).HasAttributeWithProperty<AutoApiAttribute>(a => a.OwnerCanEdit))
                return owned;

            return false;
        }

        protected virtual bool CanAddItem(Guid? sourceId)
        {
            if (Self.IsAdmin)
                return true;

            var source = GetSourceType(typeof(T));

            if (typeof(T).HasAttributeWithProperty<AutoApiAttribute>(a => a.AnyUserCanAdd))
            {
                return true;
            }

            if (source != null && sourceId.HasValue) //T has a source object
            {
                var table = typeof(T).GetProperty(SourcePropertyName).PropertyType;

                var name = this.NameOf(a => a.IsRootedIn<T>());

                //If T has owner-add permission
                if (typeof (T).HasAttributeWithProperty<AutoApiAttribute>(a => a.OwnerCanAdd))
                {
                    var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name == name);
                    var filterByType = methods.Single(mi => mi.Name == name && mi.GetGenericArguments().Length == 2);
                    var method = filterByType.MakeGenericMethod(typeof(TUser), table);
                    var isSourceOwnedBySelf = (bool)method.Invoke(this, new object[] { Self, sourceId.Value });
                    if (isSourceOwnedBySelf) return true;
                }
            }
            

            return false;
        }

        protected virtual bool CanDeleteItem(Guid id)
        {
            if (Self.IsAdmin)
                return true;

            if (typeof(T).HasAttributeWithProperty<AutoApiAttribute>(a => a.OwnerCanDelete))
                return IsRootedIn(Self, id);

            return false;
        }

        protected virtual bool CanSetValue(PropertyInfo prop)
        {
            if (Self.IsAdmin && prop.CanWrite)
                return true;

            var attr = prop.GetCustomAttribute<AutoPropertyAttribute>();

            if(attr != null) if (attr.OwnerCanSet) return true;

            return false;
        }

        protected IQueryable<T> FilterBy<TRoot, TSelection>(TRoot root, Expression<Func<TRoot, TSelection>> selector)
        {
            return FilterBy<TRoot, TSelection, T>(root, selector);
        }

        protected IQueryable<TTable> FilterBy<TRoot, TSelection, TTable>(TRoot root, Expression<Func<TRoot, TSelection>> selector) 
            where TTable : class, IIdentifiable
        {
            var memberExpression = (MemberExpression)selector.Body;
            var memberName = memberExpression.Member.Name;

            var type = typeof (TTable);
            var param = Expression.Parameter(type);

            Expression accessor = param;
            while (type != null && !typeof (TRoot).IsAssignableFrom(type))
            {
                accessor = Expression.Property(accessor, SourcePropertyName);
                type = GetSourceType(type);
            }

            if (type == null)
                return (new TTable[0]).AsQueryable();

            var body = Expression.Equal(
                Expression.PropertyOrField(accessor, memberName),
                Expression.Property(Expression.Constant(root), memberName));

            var lambda = Expression.Lambda<Func<TTable, bool>>(body, param);

            return Context.Query<TTable>().Where(lambda);
        }

        protected bool IsRootedIn<TRoot>() where TRoot : IIdentifiable
        {
            var type = typeof(T);

            while (!typeof(TRoot).IsAssignableFrom(type))
            {
                var source = type.GetProperty(SourcePropertyName);
                if (source == null) return false;
                type = source.PropertyType;
            }

            return true;
        }

        protected bool IsRootedIn<TRoot>(TRoot root, Guid id) where TRoot : IIdentifiable
        {
            return IsRootedIn<TRoot, T>(root, id);
        }

        protected bool IsRootedIn<TRoot, TTable>(TRoot root, Guid id) where TRoot : IIdentifiable where TTable : class, IIdentifiable
        {
            if (!IsRootedIn<TRoot>())
                return false;

            return FilterBy<TRoot, Guid, TTable>(root, e => e.Id).Any(r => r.Id == id);
        }

        protected TRoot FindRoot<TRoot>(Guid id) where TRoot : class, IIdentifiable
        {
            var type = typeof(T);
            var param = Expression.Parameter(typeof (T));

            Expression accessor = param;
            while (type != null && !typeof(TRoot).IsAssignableFrom(type))
            {
                accessor = Expression.Property(accessor, SourcePropertyName);
                type = GetSourceType(type);
            }

            if (type == null) 
                return null;

            var source = Context.Query<T>().SingleOrDefault(item => item.Id == id);
            if(source == null) return null;

            var lambda = Expression.Lambda<Func<T, TRoot>>(accessor, param);
            return lambda.Compile()(source);
        }

        #endregion

        #region Private

        private void TranscribeFromPayload(T item, dynamic payload)
        {
            var dict = (IDictionary<string, JToken>)payload;

            foreach (var prop in typeof(T).GetProperties())
            {

                var attr = prop.GetCustomAttribute<AutoPropertyAttribute>();
                if (attr == null) continue;

                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && typeof(string) != prop.PropertyType)
                    continue;

                var name = attr.PropertyName ?? prop.Name.Decapitalize();

                var canSet = CanSetValue(prop);

                if (canSet && dict.ContainsKey(name))
                {

                    object value = null;
                    if (dict[name].Type != JTokenType.Null)
                    {
                        value = dict[name].ToObject(prop.PropertyType);    
                    }
                    prop.SetMethod.Invoke(item, new[] { value });
                }
            }
        }

        private Type GetSourceType(Type type)
        {
            var source = type.GetProperty(SourcePropertyName);
            return source == null ? null : source.PropertyType;
        }

        #endregion
    }
}