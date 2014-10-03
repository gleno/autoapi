namespace zeco.autoapi.CodeGeneration.Generators
{
    class EntitiesGenerator : TypeScriptCodeGenerator
    {
        public override string Filename
        {
            get { return "entities.ts"; }
        }

        protected override void GenerateInternal()
        {
            Raw(string.Format(@"module {0} {{", ModuleName));
            Raw(@"

            export interface IInitializationService {
                global: any
                cache: any
                setGlobal(value:any);
            }

             export interface IEntityScope extends ng.IScope {
                 entities: any;
                 watchers: any;
                 communicators: any;
                 arrayWatchers: any;
             }
          
             export interface ICommunicator<T extends IItem> {
                 get(id: string): ng.IPromise<T>;
                 getall(): ng.IPromise<T[]>;
                 getsome(ids: string[], list?:T[]): ng.IPromise<T[]>;
                 put(entity: any): ng.IPromise<T>;
                 putmany(entities: any[]): ng.IPromise<T[]>;
                 post(entity: T): ng.IPromise<any>;
                 del(id: string, sourceId?: string): ng.IPromise<any>;
                 list: T[]
                 loadList: boolean;
             }
          
             export interface IEntityService {
                 communicator<T extends IItem>(url:string, items:any): void;
                 clear: () => void;
             }
          
             export module factories {

                export function initialization() : IInitializationService {

                    function load(container) {
                        var element = document.getElementById(container);
                        if (element != null) 
                            return JSON.parse(element.innerHTML) || {};
                        return {};
                    }

                    var service = {
                        global: load('__global'),
                        cache: load('__cache'),
                        setGlobal: function (value: any) {

                            for (var key in service.global)
                                if (service.global.hasOwnProperty(key))
                                    delete service.global[key];

                            for (var key in value)
                                if (value.hasOwnProperty(key))
                                    service.global[key] = value[key];
                        }
                    };

                    return service;
                }
          
                 function delayedUpdater(fpromise: (any) => ng.IPromise<any>, delay: number) {
                     var h1 = null;
                     var h2 = null;
                     var working = false;
          
                     function clear() {
                         clearTimeout(h1);
                         h1 = null;
                         working = false;
                     }
          
                     function run(value: any) {
          
                         function trigger() {
                             working = true;
                             fpromise(value).finally(clear);
                         }
          
                         if (!working) {
                             clear();
                         }
                         if (h1 === null) {
                             h1 = setTimeout(trigger, delay);
                         } else {
                             if (h2 != null) {
                                 clearTimeout(h2);
                             }
                             h2 = setTimeout(() => run(value));
                         }
                         return working;
                     }
          
                     return run;
                 }
          
                 export function entityService($http: ng.IHttpService, $rootScope: ng.IRootScopeService, $q: ng.IQService) {
          
                     var scope = init();
                     var registerCounter = 0;
                     var trackedLists = [];
          
                     function error() {
                         $rootScope.$broadcast('fatal-data-error');
                     }
          
                     function init() {
                         var scope = <IEntityScope><any>$rootScope.$new(true);
                         scope.entities = {};
                         scope.watchers = {};
                         scope.communicators = {};
                         scope.arrayWatchers = {};
                         registerCounter = 0;
                         return scope;
                     }

                    function cascade(sourceId: string) {

                        if (sourceId == null)
                            return;

                        var srcCommunicator = scope.communicators[sourceId];
                        if (srcCommunicator != null) srcCommunicator.get(sourceId);
                    }
          
                     function communicator<T extends IItem>(url, items:any): void {
          
                         var cominst = this;
                         var cache = <any>{};
          
                         function transcribe(old, ent) {
          
                             for (var attr in ent) {
                                 var value = ent[attr];
                                 if (value === null)
                                     old[attr] = null;
                                 else if (value instanceof Array) {
                                     old[attr].length = 0;
                                     transcribe(old[attr], value);
                                 } else if (typeof value == 'object') {
                                     transcribe(old[attr], value);
                                 } else old[attr] = ent[attr];
                             }
                         }
          
                         function register(entity: T, cascadeChanges: boolean): T {
          
                             function makeWatcher() {
                                 var updater = delayedUpdater(e => post(e), 500);
          
                                 function watchfn(n, o) {
                                     if (n !== o) updater(n);
                                 }
          
                                 return scope.$watch('entities[""' + entity.id + '""]', watchfn, true);
                             }
          
                             if (cascadeChanges)
                                 cascade(entity.sourceId);
          
                             var oldEntity = scope.entities[entity.id];
          
                             if (oldEntity === undefined) {
                                 var watcher = makeWatcher();
          
                                 scope.entities[entity.id] = entity;
                                 scope.watchers[entity.id] = watcher;
                                 scope.communicators[entity.id] = cominst;
          
                                 return entity;
                             } else {
                                 scope.watchers[entity.id]();
          
                                 transcribe(oldEntity, entity);
          
                                 var watcher = makeWatcher();
                                 scope.watchers[entity.id] = watcher;
                                 return oldEntity;
                             }
          
          
                         }
          
                         function updater<Q>(promise: ng.IPromise<{data:Q}>, process: (item: Q) => Q) {
                             $rootScope.$broadcast('updating');
          
                             var defer = $q.defer<Q>();
          
                             promise.then((result) => {
                                 defer.resolve(process(result.data));
                                 $rootScope.$broadcast('updated');
                             }).catch(error);
          
                             return defer.promise;
                         }
          
                         function get(id: string): ng.IPromise<T> {

                            if (cache[id]) {
                                 var defer = $q.defer();
                                 defer.resolve(scope.entities[id]);
                                 cache[id] = false;
                                 return defer.promise;
                             }

                             return updater<T>($http.get(url + id, {}), (e: T) => register(e, false));
                         }
          
                         function getall(): ng.IPromise<T[]> {
                             cominst.loadList = true;
          
                             function process(entities: T[]) {
                                 cominst.list.length = 0;
                                 for (var idx in entities) {
                                     var entity = entities[idx];
                                     cominst.list.push(register(entity, false));
                                 }
                                 return cominst.list;
                             }
          
                             return updater($http.get(url, {}), process);
                         }
          
                         function getsome(ids: string[], list = []): ng.IPromise<T[]> {
          
                             function process(entities: T[]) {
                                 list.length = 0;
                                 for (var idx in entities) {
                                     var entity = entities[idx];
                                     list.push(register(entity, false));
                                 }
                                 return list;
                             }
          
                             function update(idset) {
          
                                 if (!idset.length) {
                                     var dud = $q.defer();
                                     dud.resolve(list);
                                     return updater(dud.promise, process);                            
                                 }

                                 var useCache = true;
                                 var clist = [];
                                 for (var idx = 0; idx < idset.length; ++idx) {
                                     var id = idset[idx];
                                     if (cache[id]) {
                                         cache[id] = false;
                                         clist.push(scope.entities[id]);
                                     } else {
                                         useCache = false;
                                         break;
                                     }
                                 }

                                 if (useCache) {
                                     list.length = 0;
                                     for (var i = 0; i < clist.length; ++i)
                                         list.push(clist[i]);
                                     
                                     var defer = $q.defer();
                                     defer.resolve(list);
                                     return defer.promise;
                                 }

                                 var task = $http({
                                     method: 'PATCH',
                                     url: url,
                                     data: idset
                                 });
          
                                 return updater(task, process);
                             }
          
                             function setupWatcher() {

                                 var skip = true;
                                 function watchfn(n, o) {
                                     if (!skip) update(ids);
                                     skip = false;
                                 }

                                 for (var i = 0; i < trackedLists.length; ++i)
                                     if (list === trackedLists[i]) return;

                                 trackedLists.push(list);

                                 var watchId = registerCounter++;
                                 scope.arrayWatchers[watchId] = ids;
                                 scope.$watch('arrayWatchers[' + watchId + ']', watchfn, true);
                             }
          
                             
                             setupWatcher();
          
                             return update(ids);
                         }
          
                         function put(entity: any): ng.IPromise<T> {
          
                             var promise = $http.put(url, entity);
          
                             if (cominst.loadList)
                                 promise.then(() => getall());
          
                             return updater<T>(promise, (e: T) => register(e, true));
                         }

                         function putmany(entities: any[]): ng.IPromise<T[]> {

                             var promise = $http.put(url, entities);

                             if (cominst.loadList)
                                 promise.then(() => getall());

                             return updater<T[]>(promise, (es: T[]) => {
                                 var cascadePaths = {};
                                 for (var i = 0; i < es.length; ++i) {
                                     var e = es[i];
                                     cascadePaths[e.sourceId] = true;
                                     register(e, false);
                                 }

                                 for (var path in cascadePaths)
                                     cascade(path);
                                 
                                 return es;
                             });
                         }
          
                         function post(entity: T): ng.IPromise<any> {
                             return $http.post(url, entity).success((e:T) => {
                                 $rootScope.$broadcast('modified');
                             }).error(error);
                         }
          
                         function del(id: string, sourceId: string = null): ng.IPromise<void> {
          
                             var promise = $http.delete(url + id, {}).error(error);
          
                             if (cominst.loadList)
                                 promise.then(() => getall());
          
                             return promise.then(function () {

                                 if (scope.entities[id] != null) {
                                     scope.watchers[id]();
                                     delete scope.entities[id];
                                     delete scope.watchers[id];
                                     delete scope.communicators[id];
                                 }         
 
                                 cascade(sourceId);
                             });
                         }
          
                         this.list = [];
                         this.loadList = false;
          
                         this.get = get;
                         this.getall = getall;
                         this.getsome = getsome;
                         this.put = put;
                         this.post = post;
                         this.del = del;
                         this.putmany = putmany;

                         for (var id in items) {
                             cache[id] = true;
                             register(items[id], false);
                         }
                     }
          
                     var service = {
                         communicator: communicator,
                         clear: function () {
                             scope.$destroy();
                             scope = init();
                         }
                     };
          
                     return service;
          
                 }
          
                 entityService.$inject = ['$http', '$rootScope', '$q'];
          
             }
}");
        }
    }
}