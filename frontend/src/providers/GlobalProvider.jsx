import { ConfigProvider, theme as antTheme } from 'antd'
import ruRU from 'antd/locale/ru_RU'
import { ThemeProvider, useTheme } from '../contexts/ThemeContext'

const baseTokens = {
  fontSizeHeading1: 28,
  colorPrimary: '#008a5e',
  colorLink: '#008a5e'
}

/** @param {import('react').PropsWithChildren} props */
function ThemedConfigProvider(props) {
  const { isDark } = useTheme()

  return (
    <ConfigProvider
      locale={ruRU}
      theme={{
        algorithm: isDark ? antTheme.darkAlgorithm : antTheme.defaultAlgorithm,
        token: {
          ...baseTokens,
          colorPrimaryBg: isDark ? '#0d2a1c' : '#daf3e6',
        },
        components: {
          Layout: {
            headerBg: isDark ? '#141414' : '#ffffff',
            siderBg: isDark ? '#141414' : '#ffffff',
          }
        }
      }}
    >
      {props.children}
    </ConfigProvider>
  )
}

/** @param {import('react').PropsWithChildren} props */
function GlobalProvider(props) {
  return (
    <ThemeProvider>
      <ThemedConfigProvider>
        {props.children}
      </ThemedConfigProvider>
    </ThemeProvider>
  )
}

export default GlobalProvider
