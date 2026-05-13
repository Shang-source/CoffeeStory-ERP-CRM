import type { paths } from './generated/schema';

export type ApiPath = keyof paths;

export type ApiMethod<Path extends ApiPath> = Extract<{
  [Method in keyof paths[Path]]: NonNullable<paths[Path][Method]> extends never ? never : Method;
}[keyof paths[Path]], string>;

type ApiOperation<Path extends ApiPath, Method extends ApiMethod<Path>> = NonNullable<paths[Path][Method]>;

type JsonContent<Content> =
  Content extends { 'application/json': infer Json }
    ? Json
    : Content extends { 'text/json': infer Json }
      ? Json
      : Content extends { 'text/plain': infer Text }
        ? Text
        : never;

type ResponseBody<Response> = Response extends { content: infer Content } ? JsonContent<Content> : never;

type SuccessResponse<Responses> =
  Responses extends { 200: infer Response }
    ? Response
    : Responses extends { 201: infer Response }
      ? Response
      : Responses extends { 204: infer Response }
        ? Response
        : never;

type RequestBody<Request> = Request extends { content: infer Content } ? JsonContent<Content> : never;

export type ApiResponse<Path extends ApiPath, Method extends ApiMethod<Path>> =
  ApiOperation<Path, Method> extends { responses: infer Responses }
    ? ResponseBody<SuccessResponse<Responses>>
    : never;

export type ApiRequestBody<Path extends ApiPath, Method extends ApiMethod<Path>> =
  ApiOperation<Path, Method> extends { requestBody?: infer Body }
    ? RequestBody<NonNullable<Body>>
    : never;

export type ApiQuery<Path extends ApiPath, Method extends ApiMethod<Path>> =
  ApiOperation<Path, Method> extends { parameters: { query?: infer Query } }
    ? NonNullable<Query>
    : never;
