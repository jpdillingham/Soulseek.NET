import api from './api';

export const browse = ({ username }) => {
  return api.get(`/user/${username}/browse`);
};

export const getBrowseStatus = ({ username }) => {
  return api.get(`/user/${username}/browse/status`);
};