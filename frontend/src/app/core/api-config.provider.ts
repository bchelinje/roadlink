import { Provider } from '@angular/core';
import {environment} from '../../environments/environment';
import {Configuration, ConfigurationParameters} from './api';

export function apiConfigFactory(): Configuration {
  const params: ConfigurationParameters = {
    basePath: environment.apiConfig.basePath,
    withCredentials: environment.apiConfig.withCredentials,
  };
  return new Configuration(params);
}

export const API_CONFIG_PROVIDER: Provider = {
  provide: Configuration,
  useFactory: apiConfigFactory,
};
