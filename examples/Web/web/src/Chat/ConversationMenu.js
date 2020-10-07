import React from 'react';
import './Chat.css';

import {
  Icon, Button, Label, Menu
} from 'semantic-ui-react';

const ConversationMenu = ({ conversations, active, onConversationChange }) => {
  const names = Object.keys(conversations);

  const unread = Object.entries(conversations).reduce((acc, [name, messages]) => {
    acc[name] = messages.filter(message => !message.acknowledged)
    return acc;
  }, {});

  const isActive = (name) => active === name;

  return (
    <Menu className='conversation-menu' stackable size='huge'>
      {names.map((name, index) => 
        <Menu.Item
          key={index}
          style={{fontWeight: isActive(name) ? 'bold' : ''}}
          name={name}
          active={isActive(name)}
          onClick={() => onConversationChange(name)}
        >
          {name}
          {(unread[name] || []).length === 0 ? 
            '' :
            <Label color='red'>{(unread[name] || []).length}</Label>
          }
        </Menu.Item>
      )}
      <Menu.Menu position='right'>
        <Menu.Item>
          <Button icon><Icon name='plus'/></Button>
        </Menu.Item>
      </Menu.Menu>
    </Menu>
  )
}

export default ConversationMenu;