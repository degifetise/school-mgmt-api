import api from './api';

export const CONDUCT_TYPES = [
  'Merit',
  'Demerit',
  'Disciplinary Note',
  'General Behavioral Log',
];

export const conductApi = {
  async listForStudent(studentId) {
    const { data } = await api.get(`/api/conduct/student/${encodeURIComponent(studentId)}`);
    return Array.isArray(data) ? data : [];
  },

  async myStatus() {
    const { data } = await api.get('/api/conduct/my-status');
    return data;
  },

  async create(payload) {
    const { data } = await api.post('/api/conduct/logs', payload);
    return data;
  },

  async remove(id) {
    await api.delete(`/api/conduct/logs/${encodeURIComponent(id)}`);
  },
};
