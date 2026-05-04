import { instance } from './index'

export const authApi = {
  login: (data) => instance.post('/auth/login', data),
  changePassword: (data) => instance.post('/auth/change-password', data),
}
