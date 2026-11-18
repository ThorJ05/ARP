/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

export interface LoginResponse {
  token?: string;
  /** @format date-time */
  expiresAtUtc?: string;
}

export type HttpValidationProblemDetails = ProblemDetails & {
  errors?: Record<string, string[]>;
  [key: string]: any;
};

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

export interface LoginRequest {
  email?: string;
  password?: string;
}

export interface MeResponse {
  /** @format guid */
  userId?: string;
  email?: string;
  role?: string;
  /** @format guid */
  employeeId?: string | null;
  employeeName?: string | null;
}

export interface BalanceResponse {
  account?: string;
  /** @format date */
  date?: string;
  /** @format decimal */
  dayHours?: number;
  /** @format decimal */
  hours?: number;
  /** @format decimal */
  days?: number;
  /** @format decimal */
  availableHours?: number;
  /** @format decimal */
  availableDays?: number;
  /** @format decimal */
  expiredHours?: number;
  buckets?: BalanceBucketResponse[];
}

export interface BalanceBucketResponse {
  /** @format date */
  availableFrom?: string;
  /** @format date */
  expiresOn?: string;
  /** @format decimal */
  hours?: number;
  /** @format decimal */
  days?: number;
}

export interface MyRequestResponse {
  /** @format guid */
  id?: string;
  account?: string;
  /** @format date */
  startDate?: string;
  /** @format date */
  endDate?: string;
  /** @format decimal */
  hours?: number;
  status?: string;
  comment?: string | null;
  decisionReason?: string | null;
  /** @format date-time */
  createdAtUtc?: string;
  canCancel?: boolean;
}

export interface ScheduleResponse {
  /** @format guid */
  id?: string;
  /** @format date */
  validFrom?: string;
  /** @format date */
  validTo?: string | null;
  week?: WorkWeekDto;
  /** @format decimal */
  weeklyHours?: number;
  /** @format decimal */
  averageDayHours?: number;
}

export interface WorkWeekDto {
  /** @format time */
  mondayStart?: string | null;
  /** @format time */
  mondayEnd?: string | null;
  /** @format time */
  tuesdayStart?: string | null;
  /** @format time */
  tuesdayEnd?: string | null;
  /** @format time */
  wednesdayStart?: string | null;
  /** @format time */
  wednesdayEnd?: string | null;
  /** @format time */
  thursdayStart?: string | null;
  /** @format time */
  thursdayEnd?: string | null;
  /** @format time */
  fridayStart?: string | null;
  /** @format time */
  fridayEnd?: string | null;
}

export interface EmployeeResponse {
  /** @format guid */
  id?: string;
  name?: string;
  email?: string | null;
  /** @format date */
  hireDate?: string;
  /** @format date */
  terminationDate?: string | null;
  /** @format decimal */
  currentWeeklyHours?: number | null;
  /** @format decimal */
  weeks1To5Hours?: number;
  /** @format decimal */
  week6Hours?: number;
}

export interface CreatedEmployeeResponse {
  /** @format guid */
  employeeId?: string;
  /** @format guid */
  userId?: string;
}

export interface CreateEmployeeRequest {
  name?: string;
  email?: string;
  password?: string;
  /** @format date */
  hireDate?: string;
  schedule?: WorkWeekDto;
}

export interface NewScheduleRequest {
  /** @format date */
  validFrom?: string;
  schedule?: WorkWeekDto;
}

export interface TerminationRequest {
  /** @format date */
  terminationDate?: string | null;
}

export interface HolidayRequestDto {
  account?: string;
  /** @format date */
  startDate?: string;
  /** @format date */
  endDate?: string;
  comment?: string | null;
}

export interface RequestCheckResponse {
  /** @format decimal */
  hours?: number;
  /** @format int32 */
  workingDays?: number;
  errors?: string[];
}

export interface CreatedRequestResponse {
  /** @format guid */
  id?: string;
  /** @format decimal */
  hours?: number;
}

export type QueryParamsType = Record<string | number, any>;
export type ResponseFormat = keyof Omit<Body, "body" | "bodyUsed">;

export interface FullRequestParams extends Omit<RequestInit, "body"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseFormat;
  /** request body */
  body?: unknown;
  /** base url */
  baseUrl?: string;
  /** request cancellation token */
  cancelToken?: CancelToken;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown> {
  baseUrl?: string;
  baseApiParams?: Omit<RequestParams, "baseUrl" | "cancelToken" | "signal">;
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<RequestParams | void> | RequestParams | void;
  customFetch?: typeof fetch;
}

export interface HttpResponse<D extends unknown, E extends unknown = unknown>
  extends Response {
  data: D;
  error: E;
}

type CancelToken = Symbol | string | number;

export enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public baseUrl: string = "http://localhost:5288";
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private abortControllers = new Map<CancelToken, AbortController>();
  private customFetch = (...fetchParams: Parameters<typeof fetch>) =>
    fetch(...fetchParams);

  private baseApiParams: RequestParams = {
    credentials: "same-origin",
    headers: {},
    redirect: "follow",
    referrerPolicy: "no-referrer",
  };

  constructor(apiConfig: ApiConfig<SecurityDataType> = {}) {
    Object.assign(this, apiConfig);
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected encodeQueryParam(key: string, value: any) {
    const encodedKey = encodeURIComponent(key);
    return `${encodedKey}=${encodeURIComponent(typeof value === "number" ? value : `${value}`)}`;
  }

  protected addQueryParam(query: QueryParamsType, key: string) {
    return this.encodeQueryParam(key, query[key]);
  }

  protected addArrayQueryParam(query: QueryParamsType, key: string) {
    const value = query[key];
    return value.map((v: any) => this.encodeQueryParam(key, v)).join("&");
  }

  protected toQueryString(rawQuery?: QueryParamsType): string {
    const query = rawQuery || {};
    const keys = Object.keys(query).filter(
      (key) => "undefined" !== typeof query[key],
    );
    return keys
      .map((key) =>
        Array.isArray(query[key])
          ? this.addArrayQueryParam(query, key)
          : this.addQueryParam(query, key),
      )
      .join("&");
  }

  protected addQueryParams(rawQuery?: QueryParamsType): string {
    const queryString = this.toQueryString(rawQuery);
    return queryString ? `?${queryString}` : "";
  }

  private contentFormatters: Record<ContentType, (input: any) => any> = {
    [ContentType.Json]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.JsonApi]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.Text]: (input: any) =>
      input !== null && typeof input !== "string"
        ? JSON.stringify(input)
        : input,
    [ContentType.FormData]: (input: any) => {
      if (input instanceof FormData) {
        return input;
      }

      return Object.keys(input || {}).reduce((formData, key) => {
        const property = input[key];
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === "object" && property !== null
              ? JSON.stringify(property)
              : `${property}`,
        );
        return formData;
      }, new FormData());
    },
    [ContentType.UrlEncoded]: (input: any) => this.toQueryString(input),
  };

  protected mergeRequestParams(
    params1: RequestParams,
    params2?: RequestParams,
  ): RequestParams {
    return {
      ...this.baseApiParams,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...(this.baseApiParams.headers || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected createAbortSignal = (
    cancelToken: CancelToken,
  ): AbortSignal | undefined => {
    if (this.abortControllers.has(cancelToken)) {
      const abortController = this.abortControllers.get(cancelToken);
      if (abortController) {
        return abortController.signal;
      }
      return void 0;
    }

    const abortController = new AbortController();
    this.abortControllers.set(cancelToken, abortController);
    return abortController.signal;
  };

  public abortRequest = (cancelToken: CancelToken) => {
    const abortController = this.abortControllers.get(cancelToken);

    if (abortController) {
      abortController.abort();
      this.abortControllers.delete(cancelToken);
    }
  };

  public request = async <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams): Promise<HttpResponse<T, E>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.baseApiParams.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const queryString = query && this.toQueryString(query);
    const payloadFormatter = this.contentFormatters[type || ContentType.Json];
    const responseFormat = format || requestParams.format;

    return this.customFetch(
      `${baseUrl || this.baseUrl || ""}${path}${queryString ? `?${queryString}` : ""}`,
      {
        ...requestParams,
        headers: {
          ...(requestParams.headers || {}),
          ...(type && type !== ContentType.FormData
            ? { "Content-Type": type }
            : {}),
        },
        signal:
          (cancelToken
            ? this.createAbortSignal(cancelToken)
            : requestParams.signal) || null,
        body:
          typeof body === "undefined" || body === null
            ? null
            : payloadFormatter(body),
      },
    ).then(async (response) => {
      const r = response as HttpResponse<T, E>;
      r.data = null as unknown as T;
      r.error = null as unknown as E;

      const responseToParse = responseFormat ? response.clone() : response;
      const data = !responseFormat
        ? r
        : await responseToParse[responseFormat]()
            .then((data) => {
              if (r.ok) {
                r.data = data;
              } else {
                r.error = data;
              }
              return r;
            })
            .catch((e) => {
              r.error = e;
              return r;
            });

      if (cancelToken) {
        this.abortControllers.delete(cancelToken);
      }

      if (!response.ok) throw data;
      return data;
    });
  };
}

/**
 * @title ARP API
 * @version v1
 * @baseUrl http://localhost:5288
 */
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  api = {
    /**
     * No description
     *
     * @tags Auth
     * @name Login
     * @request POST:/api/auth/login
     */
    login: (data: LoginRequest, params: RequestParams = {}) =>
      this.request<
        LoginResponse,
        HttpValidationProblemDetails | ProblemDetails
      >({
        path: `/api/auth/login`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Auth
     * @name GetMe
     * @request GET:/api/auth/me
     * @secure
     */
    getMe: (params: RequestParams = {}) =>
      this.request<MeResponse, any>({
        path: `/api/auth/me`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Me
     * @name GetMyBalances
     * @request GET:/api/me/balances
     * @secure
     */
    getMyBalances: (
      query?: {
        /** @format date */
        date?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<BalanceResponse[], ProblemDetails | void>({
        path: `/api/me/balances`,
        method: "GET",
        query: query,
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Me
     * @name GetMySchedules
     * @request GET:/api/me/schedules
     * @secure
     */
    getMySchedules: (params: RequestParams = {}) =>
      this.request<ScheduleResponse[], any>({
        path: `/api/me/schedules`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Me
     * @name GetMyRequests
     * @request GET:/api/me/requests
     * @secure
     */
    getMyRequests: (params: RequestParams = {}) =>
      this.request<MyRequestResponse[], any>({
        path: `/api/me/requests`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Me
     * @name CreateRequest
     * @request POST:/api/me/requests
     * @secure
     */
    createRequest: (data: HolidayRequestDto, params: RequestParams = {}) =>
      this.request<CreatedRequestResponse, HttpValidationProblemDetails | void>(
        {
          path: `/api/me/requests`,
          method: "POST",
          body: data,
          secure: true,
          type: ContentType.Json,
          format: "json",
          ...params,
        },
      ),

    /**
     * No description
     *
     * @tags Me
     * @name PreviewRequest
     * @request POST:/api/me/requests/preview
     * @secure
     */
    previewRequest: (data: HolidayRequestDto, params: RequestParams = {}) =>
      this.request<RequestCheckResponse, HttpValidationProblemDetails | void>({
        path: `/api/me/requests/preview`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Me
     * @name CancelRequest
     * @request POST:/api/me/requests/{id}/cancel
     * @secure
     */
    cancelRequest: (id: string, params: RequestParams = {}) =>
      this.request<void, ProblemDetails | void>({
        path: `/api/me/requests/${id}/cancel`,
        method: "POST",
        secure: true,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Employees
     * @name GetEmployees
     * @request GET:/api/employees
     * @secure
     */
    getEmployees: (params: RequestParams = {}) =>
      this.request<EmployeeResponse[], any>({
        path: `/api/employees`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Employees
     * @name CreateEmployee
     * @request POST:/api/employees
     * @secure
     */
    createEmployee: (data: CreateEmployeeRequest, params: RequestParams = {}) =>
      this.request<
        CreatedEmployeeResponse,
        HttpValidationProblemDetails | ProblemDetails
      >({
        path: `/api/employees`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Employees
     * @name GetEmployeeBalances
     * @request GET:/api/employees/{id}/balances
     * @secure
     */
    getEmployeeBalances: (
      id: string,
      query?: {
        /** @format date */
        date?: string | null;
      },
      params: RequestParams = {},
    ) =>
      this.request<BalanceResponse[], ProblemDetails | void>({
        path: `/api/employees/${id}/balances`,
        method: "GET",
        query: query,
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Employees
     * @name GetScheduleHistory
     * @request GET:/api/employees/{id}/schedules
     * @secure
     */
    getScheduleHistory: (id: string, params: RequestParams = {}) =>
      this.request<ScheduleResponse[], void>({
        path: `/api/employees/${id}/schedules`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Employees
     * @name AddSchedule
     * @request POST:/api/employees/{id}/schedules
     * @secure
     */
    addSchedule: (
      id: string,
      data: NewScheduleRequest,
      params: RequestParams = {},
    ) =>
      this.request<void, HttpValidationProblemDetails | void>({
        path: `/api/employees/${id}/schedules`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Employees
     * @name SetTerminationDate
     * @request PUT:/api/employees/{id}/termination
     * @secure
     */
    setTerminationDate: (
      id: string,
      data: TerminationRequest,
      params: RequestParams = {},
    ) =>
      this.request<void, ProblemDetails | void>({
        path: `/api/employees/${id}/termination`,
        method: "PUT",
        body: data,
        secure: true,
        type: ContentType.Json,
        ...params,
      }),
  };
}
