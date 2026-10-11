module.exports = {
  root: true,
  parser: '@typescript-eslint/parser',
  parserOptions: { ecmaVersion: 2022, sourceType: 'module' },
  ignorePatterns: ['dist', 'node_modules', '.cache'],
  rules: {
    'no-unreachable': 'error',
    'no-dupe-args': 'error',
    'no-duplicate-case': 'error',
    'valid-typeof': 'error',
    'constructor-super': 'error'
  }
};
