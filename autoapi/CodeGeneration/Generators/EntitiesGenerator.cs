namespace autoapi.CodeGeneration.Generators
{
    class EntitiesGenerator : TypeScriptCodeGenerator
    {
        public override string Filename => "entities.ts";

        protected override void GenerateInternal()
        {
            Raw($@"namespace {ModuleName} {{");
            Raw(@"

            export interface IInitializationService {
                global: any;
                cache: any;
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
                 list: T[];
                 loadList: boolean;
             }
          
             export interface IEntityService {
                 communicator<T extends IItem>(url:string, typename:string, cache:any): void;
                 clear: () => void;
             }
          
             export namespace factories {

                export function initialization() : IInitializationService {

                    function load(container) {
                        const element = document.getElementById(container);
                        if (element != null) 
                            return JSON.parse(element.innerHTML) || {};
                        return {};
                    }

                    var service = {
                        global: load(""__global""),
                        cache: load(""__cache""),
                        setGlobal: (value: any) => {

                            for (let key in service.global)
                                if (service.global.hasOwnProperty(key))
                                    delete service.global[key];

                            for (let key in value)
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

                     var useSocket = false;
                     var socket = null;
                     var socketSequence = 0;
                     var socketPromises = {};

                     if ($ && ($ as any).signalR && (window as any).WebSocket) {
                        socket = ($ as any).connection(""/socket"");

                        socket.received((json: any) => {
                            const data = JSON.parse(json);
                            socketPromises[data.sequence].resolve(data);
                            delete socketPromises[data.sequence];
                        });

                        socket.stateChanged((change) => {
                            useSocket = false;
                            if (change.newState === ($ as any).signalR.connectionState.connected) {
                                useSocket = true;
                            }
                        });
                        socket.start();
                    }
          
                     function error() {
                         $rootScope.$broadcast(""fatal-data-error"");
                     }
          
                     function init() {
                         const scope = <IEntityScope><any>$rootScope.$new(true);
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

                        const srcCommunicator = scope.communicators[sourceId];
                        if (srcCommunicator != null) srcCommunicator.get(sourceId);
                    }
          
                     function communicator<T extends IItem>(url, typename, cache:any): void {

                         var cominst = this;
                         var cachem: any = {};
                         const items = (cache[typename] || {});
          
                         function transcribe(old: any, ent: any) {
                             for (let attr in ent) {
                                 if (ent.hasOwnProperty(attr)) {
                                     const value = ent[attr];
                                     if (value === null)
                                         old[attr] = null;
                                     else if (value instanceof Array) {
                                         old[attr].length = 0;
                                         transcribe(old[attr], value);
                                     } else if (typeof value == ""object"") {
                                         transcribe(old[attr], value);
                                     } else old[attr] = ent[attr];
                                 }
                             }
                         }
          
                         function register(entity: T, cascadeChanges: boolean): T {
          
                             function makeWatcher() {
                                 var updater = delayedUpdater(e => post(e), 500);

                                 function watchfn(n: any, o: any) {
                                     if (n !== o) updater(n);
                                 }

                                 return scope.$watch(`entities[""${entity.id}""]`, watchfn, true);
                             }
          
                             if (cascadeChanges)
                                 cascade(entity.sourceId);

                             const oldEntity = scope.entities[entity.id];

                             if (oldEntity === undefined) {
                                 const watcher = makeWatcher();
                                 scope.entities[entity.id] = entity;
                                 scope.watchers[entity.id] = watcher;
                                 scope.communicators[entity.id] = cominst;
                                 return entity;
                             } else {
                                 const watcher = makeWatcher();
                                 scope.watchers[entity.id]();
                                 transcribe(oldEntity, entity);
                                 scope.watchers[entity.id] = watcher;
                                 return oldEntity;
                             }
                         }

                         function flush(method: any, data: any) {
                             const def = $q.defer<any>();
                             const seq = socketSequence++;
                             socketPromises[seq] = def;
                             socket.send({
                                 method: method,
                                 data: data,
                                 sequence: seq,
                                 type: typename
                             });
                             return def.promise;
                         }

                         function request(method: string, data: any = {}) {

                             if (useSocket) 
                                 return flush(method, data);

                             return $http({
                                     method: method,
                                     url: url + (data.id || ''),
                                     data: data
                                 }).error(error);
                         }
          
                         function updater<TQ>(promise: ng.IPromise<{data:TQ}>, process: (item: TQ) => TQ) {
                             $rootScope.$broadcast(""updating"");
          
                             var defer = $q.defer<TQ>();
          
                             promise.then((result) => {
                                 defer.resolve(process(result.data));
                                 $rootScope.$broadcast(""updated"");
                             }).catch(error);
          
                             return defer.promise;
                         }
          
                         function get(id: string): ng.IPromise<T> {

                            if (cachem[id]) {
                                 const defer = $q.defer();
                                 defer.resolve(scope.entities[id]);
                                 cachem[id] = false;
                                 return defer.promise;
                             }

                             return updater<T>(request(""GET"", {id:id}), (e: T) => register(e, false));
                         }
          
                         function getall(): ng.IPromise<T[]> {
                             cominst.loadList = true;
          
                             function process(entities: T[]) {
                                 cominst.list.length = 0;
                                 for (let idx in entities) {
                                     if (entities.hasOwnProperty(idx)) {
                                         const entity = entities[idx];
                                         cominst.list.push(register(entity, false));
                                     }
                                 }
                                 return cominst.list;
                             }
          
                             return updater(request(""GET""), process);
                         }
          
                         function getsome(ids: string[], list = []): ng.IPromise<T[]> {
          
                             function process(entities: T[]) {
                                 list.length = 0;
                                 for (let idx in entities) {
                                     if (entities.hasOwnProperty(idx)) {
                                         const entity = entities[idx];
                                         list.push(register(entity, false));
                                     }
                                 }
                                 return list;
                             }
          
                             function update(idset) {
          
                                 if (!idset.length) {
                                     const dud = $q.defer();
                                     dud.resolve(list);
                                     return updater(dud.promise, process);                            
                                 }

                                 let useCache = true;
                                 const clist = [];
                                 for (let idx = 0; idx < idset.length; ++idx) {
                                     const id = idset[idx];
                                     if (cachem[id]) {
                                         cachem[id] = false;
                                         clist.push(scope.entities[id]);
                                     } else {
                                         useCache = false;
                                         break;
                                     }
                                 }

                                 if (useCache) {
                                     list.length = 0;
                                     for (let i = 0; i < clist.length; ++i)
                                         list.push(clist[i]);

                                     const defer = $q.defer();
                                     defer.resolve(list);
                                     return defer.promise;
                                 }
                                
                                 return updater(request(""PATCH"", idset), process);
                             }
          
                             function setupWatcher() {
                                 var skip = true;
                                 function watchfn() {
                                     if (!skip) update(ids);
                                     skip = false;
                                 }

                                 for (let i = 0; i < trackedLists.length; ++i)
                                     if (list === trackedLists[i]) return;

                                 trackedLists.push(list);

                                 const watchId = registerCounter++;
                                 scope.arrayWatchers[watchId] = ids;
                                 scope.$watch(`arrayWatchers[${watchId}]`, watchfn, true);
                             }
          
                             setupWatcher();
                             return update(ids);
                         }
          
                         function put(entity: any): ng.IPromise<T> {
                             const promise = request(""PUT"", entity);
                             if (cominst.loadList)
                                 promise.then(() => getall());
          
                             return updater<T>(promise, (e: T) => register(e, true));
                         }

                         function putmany(entities: any[]): ng.IPromise<T[]> {
                             const promise = request(""PUT"", entities);
                             if (cominst.loadList)
                                 promise.then(() => getall());

                             return updater<T[]>(promise, (es: T[]) => {
                                 var cascadePaths = {};
                                 for (let i = 0; i < es.length; ++i) {
                                     const e = es[i];
                                     cascadePaths[e.sourceId] = true;
                                     register(e, false);
                                 }

                                 for (let path in cascadePaths)
                                     if (cascadePaths.hasOwnProperty(path))
                                         cascade(path);

                                 return es;
                             });
                         }
          
                         function post(entity: T): ng.IPromise<any> {
                             return request(""POST"", entity).then(() => {
                                 $rootScope.$broadcast(""modified"");
                             });
                         }
          
                         function del(id: string, sourceId: string = null): ng.IPromise<void> {
          
                             const promise = request(""DELETE"", { id: id });
          
                             if (cominst.loadList)
                                 promise.then(() => getall());
          
                             return promise.then(() => {

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

                         for (let id in items) {
                             if (items.hasOwnProperty(id)) {
                                 cachem[id] = true;
                                 register(items[id], false);
                             }
                         }
                     }
          
                     const service = {
                         communicator: communicator,
                         clear: () => {
                             scope.$destroy();
                             scope = init();
                         }
                     };
          
                     return service;
          
                 }
          
                 entityService.$inject = [""$http"", ""$rootScope"", ""$q""];
             }
}");
        }
    }
}