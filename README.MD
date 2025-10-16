# Endpoints

Assembly

1. endpoint.Mapxxx();
   每个 Map 里面都是一个独立的 Application
   由 ApplicationBuilder 创建
   var appBuilder = new xxxAppliationBuilder(endpoints.CreateApplicatoinBuilder())
   var app = appBuilder.Build();

var endpointsFactory = CreateEndpointsFactory();
endpointsFactory.SetProxyPipeline(app);
endpointsFactory 中会有一个 MatchPolicy, 用来匹配可以处理的请求

反向代理是一套路由(Endpoints) Matcher 系统
匹配到的处理, 没匹配到继续往下找
最终没有找到，兜底的直接返回 404

Endpoint Match System
系统注册所有的 MatcherPolicy
Match 的时候 Foreach 判断所有 Policy 看是否可以处理

## MapAppleAppEndpoints

IEndpointRouteBuilder.MapEndpoints 实际是往当前的 Endpoints.DataSource 中添加一个新的 DataSource

DataSource.RequestProcessPipeline 是这个 Endpoints 独有的请求处理管道，这个猜测是总管道执行完成后，针对具体 Endpoints 的子管道

DataSource 要返回能处理的 Endpoint,
不同的 Endpoint 有不同的 RoutePattern
RoutePattern 只是匹配请求格式, 但是并不是绝对最终请求是否有这个 Endpoint 处理的关键步骤

这个决定的关键步骤是 MatcherPolicy 是否能成功匹配,他会检查的更多

### asp.net core mvc 的路由匹配

1. 先会按照 RoutePattern 进行地址匹配
1. 对于匹配到的地址，再通过 Matcher 来进行代码逻辑匹配, 比如检查各种 constraint

match 的时候会给出一个 candidates,

匹配到多个 candidate, 看起来会执行第一个

## CMS 要具备的能力

1. 首页设计,
2. 主题定义
3. CMS 内容的对接, 根据站点进行请求
4. 内容列表的展示
5. 内容的关联(这部分要在 CMS 中能进行关联)

综合上述能力的要求, 需要针对不同的站点进行目录规划, 渲染时到对应的目录下进行寻找相关的 Layout 以及 View 进行渲染

站点模板走独立的 ViewRender, 它只有 GET,
GET 的内容有页面/缓存生生/以及静态资源

对于站内的提交, 做统一的处理

### 要确保 publish 之后，这些 view 跟资源能加载到
