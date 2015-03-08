using System;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace zeco.autoapi
{
    public class AutoApiUserLogin : IdentityUserLogin<Guid>
    {

    }

    public class AutoApiUserRole : IdentityUserRole<Guid>
    {
        [ForeignKey("RoleId")]
        public virtual AutoApiRole AutoApiRole { get; set; }
    }

    public class AutoApiUserClaim : IdentityUserClaim<Guid>
    {
    }

    public class AutoApiRole : IdentityRole<Guid, AutoApiUserRole>
    {
        public AutoApiRole(string name)
        {
            Name = name;
        }

        public AutoApiRole()
        {
        }
    }

    public class AutoApiUserStore<TUser> : UserStore<TUser, AutoApiRole, Guid, AutoApiUserLogin, AutoApiUserRole, AutoApiUserClaim> where TUser : AutoApiUser
    {
        public AutoApiUserStore(DbContext context)
            : base(context)
        {
        }
    }

    public class AutoApiUserManager<TUser> : UserManager<TUser, Guid> where TUser : AutoApiUser
    {
        public AutoApiUserManager(AutoApiUserStore<TUser> store)
            : base(store)
        {
        }
    }

    public class AutoApiRoleStore : RoleStore<AutoApiRole, Guid, AutoApiUserRole>
    {
        public AutoApiRoleStore(DbContext context)
            : base(context)
        {
        }
    }

    public class AutoApiRoleManager : RoleManager<AutoApiRole, Guid>
    {
        public AutoApiRoleManager(AutoApiRoleStore store)
            : base(store)
        {
        }
    }

    public abstract class AutoApiDbContext<TUser> : IdentityDbContext<TUser, AutoApiRole, Guid, AutoApiUserLogin, AutoApiUserRole, AutoApiUserClaim> where TUser : AutoApiUser
    {

        public abstract string ModuleName { get;  }

        protected AutoApiDbContext() { }

        protected AutoApiDbContext(string connectionString) : base(connectionString) { }


        public T Find<T>(Guid id) where T : IIdentifiable
        {
            return (T) Find(typeof (T), id);
        }

        public object Find(Type t, Guid id)
        {
            var setType = typeof (DbSet<>).MakeGenericType(t);

            foreach (var info in GetType().GetProperties())
                if (info.PropertyType == setType)
                {
                    var set = (IQueryable<IIdentifiable>) info.GetMethod.Invoke(this, new object[0]);
                    return set.SingleOrDefault(i => i.Id == id);
                }

            throw new TypeAccessException();
        }

        public bool Undelete<T>(Guid id) where T : class, IIdentifiable
        {
            var entity = Set<T>().SingleOrDefault(item => item.Id == id);
            if (entity != null)
            {
                if (entity.IsDeleted)
                    entity.IsDeleted = false;
                return true;
            }

            return false;
        }

        public IQueryable<T> Query<T>(bool includeChildren = true) where T : class, IIdentifiable
        {
            IQueryable<T> set = Set<T>();
            if (!includeChildren) return set;

            var collections = typeof (T)
                .GetProperties()
                .Where(p => p.GetCustomAttribute<AutoPropertyAttribute>() != null)
                .Where(p => !typeof(string).IsAssignableFrom(p.PropertyType))
                .Where(p => !typeof(byte[]).IsAssignableFrom(p.PropertyType))
                .Where(p => typeof (IEnumerable).IsAssignableFrom(p.PropertyType));

            foreach (var collection in collections)
                set = set.Include(collection.Name);

            return set.Where(item => item.IsDeleted == false);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            modelBuilder.Properties<DateTime>()
                .Configure(c => c.HasColumnType("datetime2"));
        }


    }
}