import api from './api';

export const download = ({ username, filename, size }) => {
  return api.post(`/transfers/downloads/${username}`, { filename, size });
};